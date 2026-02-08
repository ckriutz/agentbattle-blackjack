public interface IPlayerStrategy
{
    Task<BetDecision> DecideBetAsync(BetContext context);
    Task<ActionDecision> DecideActionAsync(ActionContext context);
}