using Account.EventSourcing.Models;
using Functional.DotNet.Monad;
using Orleans;
using System.Reactive;

namespace Account.EventSourcing.Grains.Abstraction;

public interface IAccountGrain : IGrainWithStringKey
{
    Task<int> Deposit(decimal amount);
    Task<int> Withdraw(decimal amount);
    Task<int> Transfer(string toAccountId, decimal amount);
    Task<decimal> GetBalance();
    Task<List<string>> GetTransactionHistory();
}