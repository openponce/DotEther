# DotEther

The DotEther package allows easy integration with Ethereum blockchain, your application can create accounts, transfer funds and get balance using U.S. dollar as the basis of calculation.

### Get started

It's very easy to use the **DotEther** package in your project, it was designed to be like this;)

# Nuget

```
Install-Package DotEther
```

# Install Ganache

Quickly fire up a personal Ethereum blockchain which you can use to run tests, execute commands, and inspect state while controlling how the chain operates.

[Download Ganache](https://www.trufflesuite.com/ganache)

# How to use

### Initialize object

```
 var ethSystem = EtherSystem.Initialize(usdExchangeRate: 185.29, rpcServerUrl: "http://127.0.0.1:7545");
```

|Parameter|Type|Description|
|--|--|--|
|usdExchangeRate|double|Ethereum exchange rate in USD Dollar.|
|rpcServerUrl|string|Web3 Provider Endpoint - Default: http://localhost:8545 / Ganache: http://127.0.0.1:7545|

### Load Account

##### FromPrivateKey
You can use the private key provided by Ganache for testing.
```
var account = ethSystem.LoadFromPrivateKey("6b07d0d6870e0208afa7eea36596a085a3447e54282d3f6d95968883de7bc161");
```
To access an account using public key and password
```
var account = ethSystem.LoadFromPassword("942735b8fe1b54670e8b9dc9cdb1db0d23bed7e3", password);
```
### Get balance from an account
Balance in Ether
```
var getBalanceInEther = await ethSystem.GetBalanceInEther(account);
```

Balance in USD Dollar
```
var getBalanceInEther = await ethSystem.GetBalanceInUsd(account);
```
### Transfer funds

Transferring $ 200.00.
Funds must be transferred in US dollars.

```
var account = ethSystem.LoadFromPrivateKey("6b07d0d6870e0208afa7eea36596a085a3447e54282d3f6d95968883de7bc161");
var txHash  = await ethSystem.TransferTo(accountFrom: account, accountTo: "942735b8fe1b54670e8b9dc9cdb1db0d23bed7e3", totalInDollars: 200.00);
```

|Parameter|Type|Description|
|--|--|--|
|accountFrom|IAccount|Account that will transfer.|
|accountTo|string|Destination account address.|
|totalInDollars|double|Transfer amount in US dollar|