namespace EventSourcing.Commands;


// Business

public record BankAccount(Guid Id, decimal Amount, DateTimeOffset CreatedAt, DateTimeOffset? ClosedAt = null);

// Commands

public interface ICommand
{
};
public record OpenBankAccount(decimal InitialAmount):ICommand;

public record DepositCash(Guid AccountId,decimal Amount):ICommand;
public record WithdrawCash(Guid AccountId, decimal Amount):ICommand;

public record TransferCash(Guid FromAccountId, Guid ToAccountId, decimal Amount):ICommand;

public record CheckBalanceAccount(bool ShowDeposits, bool ShowWithdraws, DateOnly ShowUntil):ICommand;

public record CheckAccountHistory(DateOnly From, DateOnly To):ICommand;

public record CloseBankAccount(Guid AccountId):ICommand;


// Events
public abstract record Event
{
    public DateTimeOffset OccuredAt { get; } = DateTimeOffset.Now;
};
public record AccountOpened(Guid AccountId):Event;

public record CashDesposited(Guid AccountId ,decimal Amount):Event;
public record CashWithdrawn(Guid AccountId, decimal Amount):Event;

public record CashTransfered(Guid FromAccountId, Guid ToAccountId, decimal Amount):Event;



public record AccountClosed(Guid AccountId):Event;


public class State
{
    public IReadOnlyCollection<BankAccount> BankAccounts { get; init; } = [];

    public State()
    {
        
    }

    public BankAccount GetBankAccount(Guid id)
    {
        return BankAccounts.Single(b=>b.Id == id);
    }
}


public static class Decider
{
    public static IReadOnlyCollection<Event> Decide(ICommand command, State state)
    {
         switch (command)
        {
            case OpenBankAccount openBankAccount:
            {
                var accountId = Guid.NewGuid();
                return new List<Event>()
                {
                    new AccountOpened(accountId),
                    new CashDesposited(accountId,openBankAccount.InitialAmount)
                };
            }
            case DepositCash depositCash :
                return new List<Event>()
                {
                    new CashDesposited(depositCash.AccountId,depositCash.Amount),
                };
            case WithdrawCash withdrawCash :
                return new List<Event>()
                {
                    new CashWithdrawn(withdrawCash.AccountId,withdrawCash.Amount),
                };
            case TransferCash transferCash :
                return new List<Event>()
                {
                    new CashWithdrawn(transferCash.FromAccountId,transferCash.Amount),
                    new CashDesposited(transferCash.ToAccountId,transferCash.Amount),
                    new CashTransfered(transferCash.FromAccountId,transferCash.ToAccountId,transferCash.Amount),
                };
            case CloseBankAccount closeBankAccount :
                return new List<Event>()
                {
                    new CashWithdrawn(closeBankAccount.AccountId, state.GetBankAccount(closeBankAccount.AccountId).Amount),
                    new AccountClosed(closeBankAccount.AccountId),
                };
                default:
                throw new NotSupportedException($"{command.GetType().Name} cannot be handled for Bank Account");
        };
    }
}


// public record AccountOpened(Guid AccountId):Event;
// public record CashDesposited(Guid AccountId ,decimal Amount):Event;
// public record CashWithdrawn(Guid AccountId, decimal Amount):Event;
// public record CashTransfered(Guid FromAccountId, Guid ToAccountId, decimal Amount):Event;
// public record AccountClosed(Guid AccountId):Event;

public static class Evolver
{
    public static State Evolve(Event @event, State state)
    {
        switch (@event)
        {
            case AccountOpened accountOpened:
            {
                var newBankAccount = new BankAccount(accountOpened.AccountId, 0, @event.OccuredAt);
                return new State()
                {
                    BankAccounts = state.BankAccounts.Append(newBankAccount).ToList()
                };
            }
            case CashDesposited cashDesposited:
            {
                var account = state.GetBankAccount(cashDesposited.AccountId);
                return new State();
            }
            default:
                throw new NotSupportedException($"{@event.GetType().Name} cannot be evolve for Bank Account");
        };
    }
}

