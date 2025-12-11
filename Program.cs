using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Going.Plaid;
using Going.Plaid.Entity;
using Going.Plaid.Item;
using Going.Plaid.Accounts;
using Going.Plaid.Transactions;
using Going.Plaid.Sandbox;

using SystemEnvironment = System.Environment;
using PlaidEnvironment = Going.Plaid.Environment;
using DotNetEnv;

namespace spendingPotsProject
{
    class Program
    {
        static List<string> spendingpots = new List<string> { "Food", "Entertainment", "Utilities", "Savings", "Travel" };
        static List<int> potBalances = new List<int>();

        static async Task Main(string[] args)
        {
            Console.WriteLine("\nHello, Welcome to spending pots!\n");
            Console.WriteLine("This is a simple application to help you manage your spending pots.\n");

            LoadBalancesFromFile();

            Console.WriteLine("Menu:");
            Console.WriteLine("1. View Balances");
            Console.WriteLine("2. Add to Pot");
            Console.WriteLine("3. Add New Pot");
            Console.WriteLine("4. Sync from Plaid (sandbox)");
            Console.WriteLine("5. Exit");

            await MenuOptionsAsync();
        }

        static void LoadBalancesFromFile()
        {
            if (!File.Exists("spendingpots.txt"))
            {
                potBalances = new List<int> { 250, 150, 300, 500, 400 };
                Console.WriteLine("Using default balances:\n");
                DisplayBalances();
                return;
            }

            string[] lines = File.ReadAllLines("spendingpots.txt");

            var potNames = new List<string>();
            var balances = new List<int>();

            foreach (var line in lines)
            {
                var parts = line.Split(':');
                if (parts.Length < 2) continue;

                string name = parts[0].Trim();

                int dollarIndex = parts[1].IndexOf('$');
                string numberPart = (dollarIndex >= 0
                    ? parts[1].Substring(dollarIndex + 1)
                    : parts[1]).Trim();

                if (!int.TryParse(numberPart, out int balance))
                {
                    balance = 0;
                }

                potNames.Add(name);
                balances.Add(balance);
            }

            spendingpots = potNames;
            potBalances = balances;

            Console.WriteLine("Loaded pots:");
            DisplayBalances();
        }

        static async Task MenuOptionsAsync()
        {
            bool exit = false;
            while (!exit)
            {
                Console.WriteLine("\nPlease select an option (1-5):");
                string choice = Console.ReadLine() ?? "";
                switch (choice)
                {
                    case "1":
                        DisplayBalances();
                        break;
                    case "2":
                        HandleAddToPot();
                        break;
                    case "3":
                        AddNewPot();
                        break;
                    case "4":
                        await SyncFromPlaidSandboxAsync();
                        break;
                    case "5":
                        exit = true;
                        break;
                    default:
                        Console.WriteLine("Invalid option. Please try again.");
                        break;
                }
            }
        }

        static void DisplayBalances()
        {
            Console.WriteLine("\nCurrent Balances:");
            for (int i = 0; i < spendingpots.Count; i++)
            {
                Console.WriteLine(spendingpots[i] + ": $" + potBalances[i]);
            }
        }

        static void HandleAddToPot()
        {
            Console.WriteLine("\nSelect a pot to add to:");
            for (int i = 0; i < spendingpots.Count; i++)
            {
                Console.WriteLine((i + 1) + ". " + spendingpots[i]);
            }

            if (!int.TryParse(Console.ReadLine(), out int potChoice))
            {
                Console.WriteLine("Invalid number.");
                return;
            }

            int potIndex = potChoice - 1;
            if (potIndex < 0 || potIndex >= potBalances.Count)
            {
                Console.WriteLine("Invalid pot selection.");
                return;
            }

            Console.WriteLine("Enter amount to add:");
            if (!int.TryParse(Console.ReadLine(), out int amount))
            {
                Console.WriteLine("Invalid amount.");
                return;
            }

            AddToPot(potIndex, amount);
        }

        static void AddToPot(int potIndex, int amount)
        {
            potBalances[potIndex] += amount;

            Console.WriteLine("\nUpdated Balances:");
            Console.WriteLine("Added $" + amount + " to " + spendingpots[potIndex]);
            DisplayBalances();

            Console.WriteLine("\nWould you like to save the updated balances to a file? (y/n)");
            string saveChoice = Console.ReadLine() ?? "n";
            if (saveChoice.ToLower() == "y")
            {
                WriteToFile();
                Console.WriteLine("Balances saved to spendingpots.txt");
                Console.WriteLine("==================");
                Console.WriteLine("\nMenu:\n");
                Console.WriteLine("1. View Balances");
                Console.WriteLine("2. Add to Pot");
                Console.WriteLine("3. Add New Pot");
                Console.WriteLine("4. Sync from Plaid (sandbox)");
                Console.WriteLine("5. Exit\n");
                Console.WriteLine("==================");
            }
        }

        static void AddNewPot()
        {
            Console.WriteLine("\nPlease Enter The Name Of Your New Spending Pot:");
            string newPotTitle = Console.ReadLine() ?? "untitled pot";
            if (spendingpots.Contains(newPotTitle))
            {
                Console.WriteLine("A pot with that name already exists.");
                return;
            }

            Console.WriteLine("Please Enter Your Starting Balance");
            if (!int.TryParse(Console.ReadLine(), out int newPotBalance))
            {
                Console.WriteLine("Invalid amount, using 0.");
                newPotBalance = 0;
            }

            spendingpots.Add(newPotTitle);
            potBalances.Add(newPotBalance);

            using (StreamWriter file = new StreamWriter("spendingpots.txt", append: true))
            {
                file.WriteLine(newPotTitle + ": $" + newPotBalance);
            }

            Console.WriteLine("New pot added and saved.");
        }

        static void WriteToFile()
        {
            using (StreamWriter file = new StreamWriter("spendingpots.txt"))
            {
                for (int i = 0; i < spendingpots.Count; i++)
                {
                    file.WriteLine(spendingpots[i] + ": $" + potBalances[i]);
                }
            }
        }

        static PlaidClient CreatePlaidClient()
        {
            /*
             * This method creates our actual connection "client" that talks to Plaid's API.
             * Think of it like a digital access card: we load our secret keys, specify the
             * environment (sandbox in this case), and PlaidClient handles all HTTPS signing,
             * formatting, and communication for us.
             *
             * The user never sees real credentials, and Plaid never sends back sensitive
             * information unencrypted. This client simply enables our code to request
             * account balances, transactions, institutions, etc.
             */

            Env.Load();

            var clientId = SystemEnvironment.GetEnvironmentVariable("PLAID_CLIENT_ID");
            var secret = SystemEnvironment.GetEnvironmentVariable("PLAID_SECRET");

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(secret))
                throw new InvalidOperationException("Missing PLAID_CLIENT_ID or PLAID_SECRET environment variables.");

            var client = new PlaidClient(
                PlaidEnvironment.Sandbox,
                secret: secret,
                clientId: clientId);

            return client;
        }

        static async Task SyncFromPlaidSandboxAsync()
        {
            /*
             * The entire purpose of this method is to sync your spending pots with actual
             * bank account balances (in this case, sandbox test data). The flow is:
             *
             * 1) Tell Plaid to create a "public token" — this represents a simulated bank link.
             * 2) Exchange that public token for an access token — this is the key used for API calls.
             * 3) Ask Plaid for balances using that access token.
             * 4) Replace your app's pots with real account names & balances.
             *
             * In real life, step (1) is done through Plaid Link (a UI), but sandbox allows
             * us to generate everything in code.
             */

            Console.WriteLine("\nSyncing from Plaid sandbox…");

            var customNames = new List<string>();
            var customBalances = new List<int>();

            for (int i = 0; i < spendingpots.Count; i++)
            {
                if (!spendingpots[i].StartsWith("Plaid "))
                {
                    customNames.Add(spendingpots[i]);
                    customBalances.Add(potBalances[i]);
                }
            }

            var client = CreatePlaidClient();

            /*
             * This creates a simulated "public token" representing a user linking a bank
             * account in a real Plaid Link UI. In sandbox we can generate one instantly.
             */
            var sandboxReq = new SandboxPublicTokenCreateRequest
            {
                InstitutionId = "ins_109508",
                InitialProducts = new[] { Products.Transactions }
            };

            var sandboxResp = await client.SandboxPublicTokenCreateAsync(sandboxReq);

            if (sandboxResp.Error is not null)
            {
                Console.WriteLine($"Sandbox error: {sandboxResp.Error.ErrorMessage}");
                return;
            }

            string publicToken = sandboxResp.PublicToken;

            /*
             * Now we exchange the short-lived public token for an "access token".
             * The access token is what allows us to make authenticated requests like:
             *
             *   - Retrieve balances
             *   - Retrieve transactions
             *   - Retrieve identity info
             *
             * In production, this token must be stored securely.
             */
            var exchangeResp = await client.ItemPublicTokenExchangeAsync(
                new ItemPublicTokenExchangeRequest
                {
                    PublicToken = publicToken
                });

            if (exchangeResp.Error is not null)
            {
                Console.WriteLine($"Token exchange error: {exchangeResp.Error.ErrorMessage}");
                return;
            }

            string accessToken = exchangeResp.AccessToken;

            /*
             * Now that we have an access token, we can request account balances
             * Plaid will return a list of every linked account—checking, savings
             * credit cards, etc each with metadata and balance info.
             */
            var balanceResp = await client.AccountsBalanceGetAsync(
                new AccountsBalanceGetRequest
                {
                    AccessToken = accessToken
                });

            if (balanceResp.Error is not null)
            {
                Console.WriteLine($"Balance error: {balanceResp.Error.ErrorMessage}");
                return;
            }

            spendingpots.Clear();
            potBalances.Clear();

            Console.WriteLine("\nPlaid sandbox accounts:\n");

            foreach (var acc in balanceResp.Accounts)
            {
                var name = acc.Name ?? acc.OfficialName ?? "Unnamed account";
                var current = acc.Balances.Current ?? 0m;

                spendingpots.Add(name);
                potBalances.Add((int)Math.Round(current));

                Console.WriteLine($"{name}: ${current}");
            }

            for (int i = 0; i < customNames.Count; i++)
            {
                spendingpots.Add(customNames[i]);
                potBalances.Add(customBalances[i]);
            }

            WriteToFile();

            Console.WriteLine("\nSpending pots updated from Plaid sandbox (Plaid accounts + your custom pots) and saved to spendingpots.txt.");
        }
    }
}
