using System;
using System.IO;
using System.Threading.Tasks;
using CoinEx.Net;
using CoinEx.Net.Objects;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CA_Bot
{
    internal class Program
    {
        private static AppSettings Settings;

        private const decimal MinBtcAmount = 0.0001m;

        private const string Bch = "BCH";

        private static string MarketSymbol => $"{Bch}{Settings.SourceSymbol}";


        private static async Task Main(string[] args)
        {
            Log.WriteLine("Hello World!");

            #region Read AppSettings

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();
            Settings = new AppSettings();
            configuration.Bind(Settings);

            #endregion


            var clientOptions = new CoinExClientOptions();
            clientOptions.ApiCredentials = new ApiCredentials(Settings.AccessID, Settings.SecretKey);
            clientOptions.LogLevel = LogLevel.Debug;


            using (var client = new CoinExClient(clientOptions))
            {
                while (true)
                {
                    decimal hourlyAmount = Settings.SourceDailyAmount / 24m;
                    var available = await GetBalance(client, Settings.SourceSymbol);

                    await Buy(client, available > hourlyAmount ? hourlyAmount : available);

                    await WithdrawAll(client);

                    await Sleep();
                }
            }
        }

        private static async Task<decimal> GetBalance(CoinExClient client, string symbol)
        {
            var result = await client.GetBalancesAsync();
            if (!result.Success)
            {
                Log.WriteLine($"getting balance. {result.Success} {result.Error}");
                return 0;
            }

            result.Data.TryGetValue(symbol, out var balance);
            var available = balance?.Available ?? 0;

            Log.WriteLine($"available {available} {symbol}");
            return available;
        }

        private static async Task WithdrawAll(CoinExClient client)
        {
            var balance = await GetBalance(client, Bch);

            await Withdraw(client, balance);
        }

        private static async Task Withdraw(CoinExClient client, decimal amount)
        {
            Log.WriteLine($"withdrawing {amount} {Bch}");
            var result = await client.WithdrawAsync(Bch, Settings.WithdrawalAddress, false, amount);
            Log.WriteLine($"{result.Success} {result.Error}");
        }

        private static async Task Buy(CoinExClient client, decimal amount)
        {
            //var market = client.GetMarketInfo(MarketSymbol).Data[MarketSymbol];
            //var minAmount = market.MinAmount;
            //Log.WriteLine($"minAmount {minAmount}");

            Log.WriteLine($"buying for {amount} {Settings.SourceSymbol}");
            var result = await client.PlaceMarketOrderAsync(MarketSymbol, TransactionType.Buy, amount);
            Log.WriteLine($"{result.Success} {result.Data?.ExecutedAmount} {result.Error}");
        }

        private static async Task Sleep()
        {
            Console.Write("sleeping ");

            for (int i = 0; i < 60; i++)
            {
                await Task.Delay(TimeSpan.FromMinutes(1));
                Console.Write(".");
            }

            Console.WriteLine();
        }
    }
}