using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using DigitalBusSystem.Data;
using DigitalBusSystem.Models;
using DigitalBusSystem.Services;

namespace DigitalBusSystem.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly JwtTokenService _tokenService;
        private readonly string _adminRegistrationKey;

        public UsersController(AppDbContext db, JwtTokenService tokenService, IConfiguration configuration)
        {
            _db = db;
            _tokenService = tokenService;
            _adminRegistrationKey = configuration["AdminRegistrationKey"] ?? string.Empty;
        }

        private int? GetCurrentUserId()
        {
            var value = User.Claims
                .Where(c => c.Type == ClaimTypes.NameIdentifier
                            || c.Type == JwtRegisteredClaimNames.UniqueName
                            || c.Type == JwtRegisteredClaimNames.Sub
                            || c.Type == ClaimTypes.Name)
                .Select(c => c.Value)
                .FirstOrDefault(v => int.TryParse(v, out _));

            return int.TryParse(value, out var id) ? id : null;
        }

        private bool IsAdmin() => User.IsInRole("Admin");

        private bool IsUserLoggedIn(User user) => _db.UserSessions.Any(s => s.UserId == user.Id && s.ExpiresAt > DateTime.UtcNow);

        private static UserResponse ToUserResponse(User user, bool isLoggedIn) => new UserResponse
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Language = user.Language,
            IsAdmin = user.IsAdmin,
            IsLoggedIn = isLoggedIn
        };

        private string? GetCurrentSessionId() => User.FindFirstValue(JwtRegisteredClaimNames.Jti);

        // POST: api/Users/register
        [HttpPost("register")]
        [AllowAnonymous]
        public ActionResult<UserResponse> Register(RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Password is required.");
            }

            if (_db.Users.Any(u => u.Email == request.Email)) return BadRequest("Email already exists");

            if (request.IsAdmin)
            {
                if (string.IsNullOrWhiteSpace(_adminRegistrationKey))
                {
                    return BadRequest("Admin registration is disabled. Configure AdminRegistrationKey to enable admin creation.");
                }

                if (request.AdminRegistrationKey != _adminRegistrationKey)
                {
                    return BadRequest("Invalid admin registration key.");
                }
            }

            var user = new User
            {
                Name = request.Name,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                Language = request.Language,
                SecurityQuestion = request.SecurityQuestion,
                SecurityAnswer = request.SecurityAnswer,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                IsLoggedIn = false,
                IsAdmin = request.IsAdmin
            };

            _db.Users.Add(user);
            _db.SaveChanges();
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, ToUserResponse(user, false));
        }

        // POST: api/Users/login
        [HttpPost("login")]
        [AllowAnonymous]
        public ActionResult<AuthResponse> Login(LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Password)) return BadRequest("Password is required.");

            var user = _db.Users.FirstOrDefault(u => u.Email == request.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash)) return Unauthorized("Invalid login");

            var sessionId = Guid.NewGuid().ToString();
            var expiresAt = DateTime.UtcNow.AddMinutes(_tokenService.ExpiresInMinutes);
            var session = new UserSession
            {
                UserId = user.Id,
                SessionId = sessionId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = expiresAt
            };

            _db.UserSessions.Add(session);
            user.IsLoggedIn = true;
            _db.SaveChanges();

            var token = _tokenService.CreateToken(user, sessionId);
            return Ok(new AuthResponse
            {
                Token = token,
                ExpiresAt = expiresAt
            });
        }

        // POST: api/Users/logout/1
        [HttpPost("logout/{id}")]
        public ActionResult Logout(int id)
        {
            var currentUserId = GetCurrentUserId();
            if (!IsAdmin() && currentUserId != id)
            {
                return Forbid();
            }

            var user = _db.Users.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound();

            if (currentUserId == id)
            {
                var currentSessionId = GetCurrentSessionId();
                if (!string.IsNullOrWhiteSpace(currentSessionId))
                {
                    var currentSession = _db.UserSessions.FirstOrDefault(s => s.UserId == id && s.SessionId == currentSessionId);
                    if (currentSession != null)
                    {
                        _db.UserSessions.Remove(currentSession);
                    }
                }
            }
            else if (IsAdmin())
            {
                var sessions = _db.UserSessions.Where(s => s.UserId == id).ToList();
                _db.UserSessions.RemoveRange(sessions);
            }

            var hasActiveSessions = _db.UserSessions.Any(s => s.UserId == id && s.ExpiresAt > DateTime.UtcNow);
            user.IsLoggedIn = hasActiveSessions;
            _db.SaveChanges();

            return Ok("Logged out");
        }

        // PUT: api/Users/1
        [HttpPut("{id}")]
        public ActionResult<UserResponse> UpdateProfile(int id, User updatedUser)
        {
            var currentUserId = GetCurrentUserId();
            if (!IsAdmin() && currentUserId != id)
            {
                return Forbid();
            }

            var user = _db.Users.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound();
            user.Name = updatedUser.Name;
            user.PhoneNumber = updatedUser.PhoneNumber;
            user.Language = updatedUser.Language;
            _db.SaveChanges();
            return Ok(ToUserResponse(user, IsUserLoggedIn(user)));
        }

        // POST: api/Users/change-password
        [HttpPost("change-password")]
        public ActionResult ChangePassword(int id, string oldPassword, string newPassword)
        {
            var currentUserId = GetCurrentUserId();
            if (!IsAdmin() && currentUserId != id)
            {
                return Forbid();
            }

            var user = _db.Users.FirstOrDefault(u => u.Id == id);
            if (user == null || !BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash)) return BadRequest("Wrong password");
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _db.SaveChanges();
            return Ok("Password changed");
        }

        // POST: api/Users/reset-password
        [HttpPost("reset-password")]
        public ActionResult ResetPassword(string email, string securityAnswer, string newPassword)
        {
            var user = _db.Users.FirstOrDefault(u => u.Email == email);
            if (user == null || user.SecurityAnswer != securityAnswer) return BadRequest("Wrong answer");
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _db.SaveChanges();
            return Ok("Password reset");
        }

        [HttpGet("{id:int}")]
        public ActionResult<UserResponse> GetUser(int id)
        {
            var user = _db.Users.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound();

            var currentUserId = GetCurrentUserId();
            if (!User.IsInRole("Admin") && currentUserId != id)
            {
                return Forbid();
            }

            return Ok(ToUserResponse(user, IsUserLoggedIn(user)));
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public ActionResult<IEnumerable<UserResponse>> GetAllUsers()
        {
            var users = _db.Users.ToList();
            return Ok(users.Select(u => ToUserResponse(u, IsUserLoggedIn(u))).ToList());
        }

        [HttpGet("admins")]
        [Authorize(Roles = "Admin")]
        public ActionResult<IEnumerable<UserResponse>> GetAdminUsers()
        {
            var users = _db.Users.Where(u => u.IsAdmin).ToList();
            return Ok(users.Select(u => ToUserResponse(u, IsUserLoggedIn(u))).ToList());
        }

        [HttpGet("me")]
        public ActionResult<UserResponse> GetCurrentUser()
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null) return Unauthorized();

            var user = _db.Users.FirstOrDefault(u => u.Id == currentUserId.Value);
            if (user == null) return NotFound();
            return Ok(ToUserResponse(user, IsUserLoggedIn(user)));
        }

        [HttpPost("promote/{id}")]
        [Authorize(Roles = "Admin")]
        public ActionResult PromoteUser(int id)
        {
            var user = _db.Users.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound();
            if (user.IsAdmin) return BadRequest("User is already an admin.");

            user.IsAdmin = true;
            _db.SaveChanges();
            return Ok(ToUserResponse(user, IsUserLoggedIn(user)));
        }

        [HttpPost("demote/{id}")]
        [Authorize(Roles = "Admin")]
        public ActionResult DemoteUser(int id)
        {
            var user = _db.Users.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound();
            if (!user.IsAdmin) return BadRequest("User is not an admin.");

            var currentUserId = GetCurrentUserId();
            if (currentUserId == id) return BadRequest("Admins cannot demote themselves.");

            user.IsAdmin = false;
            _db.SaveChanges();
            return Ok(ToUserResponse(user, IsUserLoggedIn(user)));
        }
    }
}