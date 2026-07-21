using System.Net.Http;
using System.Net.Http.Json;

namespace DigitalBusClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("http://localhost:5013/api/");

            while (true)
            {
                Console.WriteLine("\n===== DIGITAL BUS SYSTEM =====");
                Console.WriteLine("1. Create Bus Card");
                Console.WriteLine("2. TopUp Card");
                Console.WriteLine("3. Buy Ticket");
                Console.WriteLine("4. Validate Ticket");
                Console.WriteLine("5. Exit System");
                Console.Write("Choose an option: ");
                
                string choice = Console.ReadLine()!;

                if (choice == "1")
                {
                    Console.Write("Enter your Name: ");
                    string name = Console.ReadLine()!;
                    Console.Write("Enter your Email: ");
                    string email = Console.ReadLine()!;
                    Console.Write("Enter your Phone: ");
                    string phone = Console.ReadLine()!;
                    Console.Write("Create a Password: ");
                    string password = Console.ReadLine()!;

                    var user = new { 
                        name, email, phoneNumber = phone, 
                        passwordHash = password, language = "en",
                        securityQuestion = "Pet?", securityAnswer = "Dog", isLoggedIn = false 
                    };

                    var res = await client.PostAsJsonAsync("Users/register", user);
                    string result = await res.Content.ReadAsStringAsync();
                    Console.WriteLine(res.IsSuccessStatusCode ? $"Card Created! {result}" : $"Error: {result}");
                }
                else if (choice == "2") // TOPUP
                {
                    Console.Write("Enter User ID: ");
                    int id = int.Parse(Console.ReadLine()!);
                    Console.Write("Enter Amount: ");
                    decimal amount = decimal.Parse(Console.ReadLine()!);
                    
                    var res = await client.PostAsync($"Wallets/topup?userId={id}&amount={amount}", null);
                    string result = await res.Content.ReadAsStringAsync();
                    Console.WriteLine(res.IsSuccessStatusCode ? $"TopUp Successful! {result}" : $"Error: {result}");
                }
                else if (choice == "3") // BUY TICKET
                {
                    Console.Write("Enter Buyer User ID: ");
                    int id = int.Parse(Console.ReadLine()!);
                    Console.Write("Enter Route ID: ");
                    int route = int.Parse(Console.ReadLine()!);
                    Console.Write("Enter Price: ");
                    decimal price = decimal.Parse(Console.ReadLine()!);
                    
                    var res = await client.PostAsync($"Tickets/buy?buyerUserId={id}&routeId={route}&price={price}", null);
                    string result = await res.Content.ReadAsStringAsync();
                    Console.WriteLine(res.IsSuccessStatusCode ? $"Ticket Bought! {result}" : $"Error: {result}");
                }
                else if (choice == "4") // VALIDATE
                {
                    Console.Write("Enter Ticket Code: ");
                    string code = Console.ReadLine()!;
                    var res = await client.PostAsync($"Tickets/validate/{code}", null);
                    string result = await res.Content.ReadAsStringAsync();
                    Console.WriteLine(res.IsSuccessStatusCode ? $"Validation: {result}" : $"Error: {result}");
                }
                else if (choice == "5") break;
                else Console.WriteLine("Invalid choice");
            }
        }
    }
}