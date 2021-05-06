using System;
using System.IO;
using System.Threading.Tasks;
using CoinEx.Net;
using CoinEx.Net.Objects;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Logging;
using Microsoft.Extensions.Configuration;

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

            //clientOptions.LogVerbosity = LogVerbosity.Debug;
            //var streamWriter = new StreamWriter("log.txt");
            //streamWriter.AutoFlush = true;
            //clientOptions.LogWriters.Add(streamWriter);


            using (var client = new CoinExClient(clientOptions))
            {
                while (true)
                {
                    //WithdrawAll(client);

                    decimal HourlyAmount = Settings.SourceDailyAmount / 24m;
                    Buy(client, HourlyAmount);

                    await Sleep();
                }
            }
        }

        private static void WithdrawAll(CoinExClient client)
        {
            var balances = client.GetBalances();
            if (!balances.Success)
            {
                Log.WriteLine($"Failed to get {nameof(balances)}: " + balances.Error);
                return;
            }

            var balance = balances.Data[Bch];
            Log.WriteLine(string.Join(Environment.NewLine, balance.Available));

            Withdraw(client, balance.Available);
        }

        private static void Withdraw(CoinExClient client, decimal amount)
        {
            Log.WriteLine($"withdrawing {amount}");
            var result = client.Withdraw(Bch, Settings.WithdrawalAddress, false, amount);
            Log.WriteLine($"{result.Success} {result.Error}");
        }

        private static void Buy(CoinExClient client, decimal amount)
        {
            //var market = client.GetMarketInfo(MarketSymbol).Data[MarketSymbol];
            //var minAmount = market.MinAmount;
            //Log.WriteLine($"minAmount {minAmount}");


            Log.WriteLine($"buying for {amount} {Settings.SourceSymbol}");
            var result = client.PlaceMarketOrder(MarketSymbol, TransactionType.Buy, amount);
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