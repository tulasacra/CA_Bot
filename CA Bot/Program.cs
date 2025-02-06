using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CoinEx.Net.Clients;
using CoinEx.Net.Enums;
using CryptoExchange.Net.Authentication;
using Microsoft.Extensions.Configuration;

namespace CA_Bot
{
    internal class Program
    {
        private static AppSettings Settings;
        private const string DestinationSymbol = "BCH";

        private static string MarketSymbol => $"{DestinationSymbol}{Settings.SourceSymbol}";


        private static async Task Main(string[] args)
        {
            #region Read AppSettings

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();
            Settings = new AppSettings();
            configuration.Bind(Settings);

            #endregion


            var apiCredentials = new ApiCredentials(Settings.AccessID, Settings.SecretKey);

            using (var client = new CoinExRestClient(options =>  options.ApiCredentials = apiCredentials))
            {
                while (true)
                {
                    decimal hourlyAmount = Settings.SourceDailyAmount / 24m;
                    var availableSource = await GetBalance(client, Settings.SourceSymbol);

                    var amountToSpend = availableSource > 2 * hourlyAmount ? hourlyAmount : availableSource;
                    await Buy(client, amountToSpend);

                    await WithdrawAll(client, availableSource == amountToSpend);

                    await Sleep();
                }
            }
        }

        private static async Task<decimal> GetBalance(CoinExRestClient client, string symbol)
        {
            var result = await client.SpotApiV2.Account.GetBalancesAsync();
            if (!result.Success)
            {
                Log.WriteLine($"getting balance. {result.Success} {result.Error}");
                return 0;
            }

            var balance = result.Data?.First(balance => balance.Asset == symbol);
            var available = balance?.Available ?? 0;

            Log.WriteLine($"available {available} {symbol}");
            return available;
        }

        private static async Task WithdrawAll(CoinExRestClient client, bool overrideMinimumWithdrawalAmount)
        {
            var balance = await GetBalance(client, DestinationSymbol);

            if (balance >= Settings.MinimumWithdrawalAmount || overrideMinimumWithdrawalAmount)
            {
                const decimal fee = 0.00001000m;

                await Withdraw(client, balance - fee);
            }
        }

        private static async Task Withdraw(CoinExRestClient client, decimal amount)
        {
            Log.WriteLine($"withdrawing {amount} {DestinationSymbol}");
            var result = await client.SpotApiV2.Account.WithdrawAsync(DestinationSymbol, amount, Settings.WithdrawalAddress, MovementMethod.OnChain, DestinationSymbol);
            Log.WriteLine($"{result.Success} {result.Error}");
        }

        private static async Task Buy(CoinExRestClient client, decimal amount)
        {
            //var market = client.GetMarketInfo(MarketSymbol).Data[MarketSymbol];
            //var minAmount = market.MinAmount;
            //Log.WriteLine($"minAmount {minAmount}");

            Log.WriteLine($"buying for {amount} {Settings.SourceSymbol}");
            var result = await client.SpotApiV2.Trading.PlaceOrderAsync(
                MarketSymbol,
                AccountType.Spot,
                OrderSide.Buy,
                OrderTypeV2.Market,
                amount,
                quantityAsset: Settings.SourceSymbol);
            Log.WriteLine($"{result.Success} {result.Data?.QuantityFilled} {result.Error}");
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