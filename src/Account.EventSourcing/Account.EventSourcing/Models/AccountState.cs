namespace Account.EventSourcing.Models;


[Serializable]
public class AccountState
{
    public decimal Balance { get; set; }
}