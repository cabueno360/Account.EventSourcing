using Account.EventSourcing.Grains.Abstraction;
using Account.EventSourcing.Models;
using Account.EventSourcing.Grains.Abstraction;
using Account.EventSourcing.Models;
using static Functional.DotNet.F;
using Functional.DotNet.Monad;
using Orleans;
using System.Reactive;

namespace Account.EventSourcing.Grains;
public class AccountGrain : Grain, IAccountGrain
{

    private readonly ILogger<AccountGrain> _logger;
    private readonly IPersistentState<AccountState> _state;
    private readonly IPersistentState<List<AccountEvent>> _eventJournal;

    public AccountGrain(
        [PersistentState("accountState", "accountStore")] IPersistentState<AccountState> state,
        [PersistentState("eventJournal", "eventStore")] IPersistentState<List<AccountEvent>> eventJournal,
        ILogger<AccountGrain> logger)
    {
        _state = state;
        _eventJournal = eventJournal;
        _logger = logger;
    }

    public const int Void = 0;
    public async Task<int> Deposit(decimal amount)
    {
        if (amount <= 0)
            throw new InvalidOperationException("Deposit amount must be positive.");

        var evt = new DepositEvent(amount);
        await ApplyEvent(evt);

        return Void;
    }

    public async Task<int> Withdraw(decimal amount)
    {
        if (amount <= 0)
            throw new InvalidOperationException("Withdrawal amount must be positive.");

        if (_state.State.Balance < amount)
            throw new InvalidOperationException("Insufficient funds.");

        var evt = new WithdrawEvent(amount);
        await ApplyEvent(evt);

        return Void;
    }

    public async Task<int> Transfer(string toAccountId, decimal amount)
    {
        if (string.IsNullOrEmpty(toAccountId))
            throw new ArgumentNullException(nameof(toAccountId));

        if (amount <= 0)
            throw new InvalidOperationException("Transfer amount must be positive.");

        await Withdraw(amount);

        var toAccount = GrainFactory.GetGrain<IAccountGrain>(toAccountId);
        await toAccount.Deposit(amount);

        var evt = new TransferEvent(toAccountId, amount);
        await ApplyEvent(evt);

        return Void;
    }

    public Task<decimal> GetBalance()
    {
        return Task.FromResult(_state.State.Balance);
    }

    public Task<List<string>> GetTransactionHistory()
    {
        var events  = _eventJournal.State.Select(x=> x switch { 
            DepositEvent ev=> $"Deposit of {ev.Amount}",
            WithdrawEvent ev => $"Withdraw of {ev.Amount}",
            _ => ""
        }).ToList();
        return Task.FromResult(events);
    }

    private async Task<Unit> ApplyEvent(AccountEvent evt)
    {

        _state.State.Balance = evt switch
        {
            DepositEvent deposit => _state.State.Balance + deposit.Amount,
            WithdrawEvent withdraw => _state.State.Balance - withdraw.Amount,
            TransferEvent transfer => _state.State.Balance,
        };

        _eventJournal.State.Add(evt);

        await _state.WriteStateAsync();
        await _eventJournal.WriteStateAsync();

        _logger.LogInformation($"Event applied: {evt}");

        return Unit();
    }
}