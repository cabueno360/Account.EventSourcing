using Orleans.Concurrency;

namespace Account.EventSourcing.Models;

[Immutable]
[GenerateSerializer]
public abstract record AccountEvent;

[Immutable]
[GenerateSerializer]
public record DepositEvent([property: Id(0)] decimal Amount) : AccountEvent;

[Immutable]
[GenerateSerializer]
public record WithdrawEvent([property: Id(0)] decimal Amount) : AccountEvent;

[Immutable]
[GenerateSerializer]
public record TransferEvent(
    [property: Id(0)] string ToAccountId,
    [property: Id(1)] decimal Amount) : AccountEvent;