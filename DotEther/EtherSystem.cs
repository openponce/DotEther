namespace DotEther.Application.Ethereum
{
    using System;
    using DotEther.Domain.Interfaces;
    using Nethereum.Util;
    using Nethereum.Web3;
    using Nethereum.Web3.Accounts;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Numerics;
    using System.Threading.Tasks;
    using static Nethereum.Util.UnitConversion;
    using DotEther.Domain.Constants;
    using Newtonsoft.Json;
    using Nethereum.Hex.HexTypes;
    using Nethereum.Hex.HexConvertors.Extensions;
    using Nethereum.Web3.Accounts.Managed;
    using DotEther.Domain.Models.Ethereum;
    using DotEther.Domain.Models.Exchanges;
    using Nethereum.RPC.Accounts;

    /// <summary>
    /// 
    /// </summary>
    public class EtherSystem : IEtherSystem
    {

        /// <summary>
        /// 
        /// </summary>
        private readonly double UsdExchangeRate;
        private readonly string RpcServerUrl;

        /// <summary>
        /// 
        /// </summary>
        public EtherSystem(double usdExchangeRate, string rpcServerUrl)
        {
            RpcServerUrl = rpcServerUrl;
            UsdExchangeRate = usdExchangeRate;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static IEtherSystem Initialize(double usdExchangeRate, string rpcServerUrl = "http://localhost:8545")
        {
            var myWallet = new EtherSystem(usdExchangeRate, rpcServerUrl);
            return myWallet;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        public static async Task<string> NewAccount(string password, string rpcServerUrl = "http://localhost:8545")
        {
            return await new Web3(url: rpcServerUrl).Personal.NewAccount.SendRequestAsync(password);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public IAccount LoadFromPassword(string address, string password)
        {
            var managedAccount = new ManagedAccount(address, password);
            return managedAccount.TransactionManager.Account;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="privateKey"></param>
        /// <returns></returns>
        public IAccount LoadFromPrivateKey(string privateKey)
        {
            return new Account(privateKey);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="keyStoreEncryptedJson"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public IAccount LoadFromKeyStore(string keyStoreEncryptedJson, string password)
        {
            return Account.LoadFromKeyStore(keyStoreEncryptedJson, password);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="totalInDollars"></param>
        /// <returns></returns>
        public BigDecimal FromUsdToEther(double totalInDollars)
        {
            var weis = FromUsdToWei(totalInDollars);
            return UnitConversion.Convert.FromWeiToBigDecimal(weis, EthUnit.Ether);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="totalInDollars"></param>
        /// <returns></returns>
        public BigInteger FromUsdToWei(double totalInDollars)
        {
            //Obtém e arredonda o valor para cima
            BigInteger _totalInDollars = new BigInteger(System.Math.Ceiling(totalInDollars));
            //Obtem o valor do ether em dollar
            var getEtherPriceInUsd = GetEtherUnitPriceInUsd();
            //Converto o valor para centavos
            var convertToCents = new BigInteger(getEtherPriceInUsd * 100);
            //Obtem WEIS equivalentes a 1 centavo de dolar
            var onePenny = BigInteger.Divide(new BigInteger(DotEther.Domain.Constants.Constants.ETHER), convertToCents);
            //O valor de 1 dolar em WEI (1 dolar = 100 centavos)
            var oneDollar = BigInteger.Multiply(onePenny, new BigInteger(100));
            //Multiplica o valor 1 dolar pelo total de dolares que serão enviados
            return BigInteger.Multiply(oneDollar, _totalInDollars);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="totalInEther"></param>
        /// <returns></returns>
        public BigDecimal FromEtherToUsd(BigDecimal totalInEther)
        {
            var getEtherPriceInUsd = GetEtherUnitPriceInUsd();
            return totalInEther * getEtherPriceInUsd;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="totalWeis"></param>
        /// <returns></returns>
        public BigDecimal FromWeiToEther(BigInteger totalWeis)
        {
            return Web3.Convert.FromWei(totalWeis, EthUnit.Ether);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public double GetEtherUnitPriceInUsd()
        {
            return UsdExchangeRate;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static async Task<Exchange> GetEtherExchangeRateInUsd()
        {
            try
            {
                WebClient webClient = new WebClient();
                string jsonResponse = await webClient.DownloadStringTaskAsync(Constants.EXCHANGE_API_URL);
                return JsonConvert.DeserializeObject<Domain.Models.Exchanges.Exchange>(jsonResponse);
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<BigInteger> GetBalanceInWei(IAccount account)
        {
            var balance = await Web3Instance(account).Eth.GetBalance.SendRequestAsync(account.Address.ToString());
            return balance.Value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<BigDecimal> GetBalanceInEther(IAccount account)
        {
            return FromWeiToEther(await GetBalanceInWei(account));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<double> GetBalanceInUsd(IAccount account)
        {
            var getBalance = await GetBalanceInEther(account);
            var getEtherPriceInUsd = GetEtherUnitPriceInUsd();
            var balance = getBalance * getEtherPriceInUsd;
            return double.Parse(balance.ToString(), GetCultureInfo());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="toAddress"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<BigInteger> EstimateOperatingCostInWei(IAccount account, string toAddress, BigInteger value)
        {
            return BigInteger.Add(value, await EstimateGas(account, toAddress, value));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="toAddress"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public double EstimateOperatingCostInUsd(BigInteger value)
        {
            return double.Parse(FromEtherToUsd(FromWeiToEther(value)).ToString(), GetCultureInfo());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="toAddress"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public async Task<BigInteger> EstimateGas(IAccount account, string toAddress, BigInteger value)
        {
            ///
            var callInput = new Nethereum.RPC.Eth.DTOs.CallInput(null, toAddress, new HexBigInteger(value));
            //
            var gasPrice = await Web3Instance(account)
                                    .Eth
                                    .GasPrice
                                    .SendRequestAsync();
            //
            var estimatedGas = (await Web3Instance(account)
                                         .Eth
                                         .Transactions
                                         .EstimateGas
                                         .SendRequestAsync(callInput)).Value;
            //
            return BigInteger.Multiply(gasPrice, estimatedGas);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="toAddress"></param>
        /// <param name="totalWei"></param>
        /// <returns></returns>
        public async Task<TransactionResult> TransferTo(IAccount accountFrom, string accountTo, double totalInDollars)
        {
            try
            {
                var totalWei = FromUsdToWei(totalInDollars);
                var estimateGas = await EstimateGas(accountFrom, accountTo, totalWei);
                var balanceFrom = await GetBalanceInWei(accountFrom);
                var operatingCostWei = await EstimateOperatingCostInWei(accountFrom, accountTo, new HexBigInteger(totalWei));
                var operatingCostUsd = EstimateOperatingCostInUsd(operatingCostWei);
                var getEtherPriceInUsd = GetEtherUnitPriceInUsd();

                //Se a taxa de transação deve incidir sobre quem está pagando...
                if (balanceFrom >= operatingCostWei)
                {
                    var token = await Web3Instance(accountFrom)
                                    .TransactionManager
                                    .SendTransactionAsync(accountFrom.Address, accountTo, new HexBigInteger(totalWei));

                    //
                    return new TransactionResult
                    {
                        From = accountFrom.Address.ToString(),
                        To = accountTo,
                        EstimatedGas = estimateGas,
                        OperatingCostWei = operatingCostWei,
                        OperatingCostUsd = operatingCostUsd,
                        TxHash = token,
                        TransactionStatus = Domain.Enums.TransactionStatus.Done,
                        TransactionDetails = Domain.Enums.TransactionDetails.Successfully,
                        Message = "operation successfully completed",
                        ValueWei = totalWei,
                        ValueUsd = totalInDollars,
                        EtherPriceInUsd = getEtherPriceInUsd
                    };

                }
                else
                {
                    return new TransactionResult
                    {
                        From = accountFrom.Address.ToString(),
                        To = accountTo,
                        EstimatedGas = estimateGas,
                        OperatingCostWei = operatingCostWei,
                        OperatingCostUsd = operatingCostUsd,
                        TxHash = null,
                        TransactionStatus = Domain.Enums.TransactionStatus.Fail,
                        TransactionDetails = Domain.Enums.TransactionDetails.InsufficientFunds,
                        Message = "Insufficient funds",
                        ValueWei = totalWei,
                        ValueUsd = totalInDollars,
                        EtherPriceInUsd = getEtherPriceInUsd
                    };
                }
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        private Web3 Web3Instance(IAccount account)
        {
            return new Web3(account, url: RpcServerUrl);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private CultureInfo GetCultureInfo()
        {
            return new CultureInfo("en-US");
        }
    }

}