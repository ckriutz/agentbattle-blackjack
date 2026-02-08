public class RegularPlayerStrategy : IPlayerStrategy
{
    public Task<BetDecision> DecideBetAsync(BetContext context)
    {
        // Conservative betting: 1-2 units based on position
        int units = context.Balance > context.MinBet * 20 ? 2 : 1;
        int amount = context.MinBet * units;
        amount = Math.Min(amount, context.Balance);
        
        var reasoning = units > 1 
            ? "Comfortable bankroll, betting 2 units" 
            : "Conservative 1-unit bet to preserve bankroll";
        
        return Task.FromResult(new BetDecision(amount, reasoning));
    }

    public Task<ActionDecision> DecideActionAsync(ActionContext context)
    {
        // Basic strategy decision tree
        var action = DecideAction(context);
        var reasoning = GetReasoning(context, action);
        
        return Task.FromResult(new ActionDecision(action, reasoning));
    }

    private PlayerAction DecideAction(ActionContext ctx)
    {
        // Already done
        if (ctx.HandValue >= 21) return PlayerAction.Stand;
        
        var dealerValue = GetDealerUpCardValue(ctx.DealerUpCard);
        
        // Double down logic (only on initial 2 cards)
        if (ctx.CanDoubleDown)
        {
            // Double on 11 always
            if (ctx.HandValue == 11) return PlayerAction.DoubleDown;
            
            // Double on 10 if dealer shows 2-9
            if (ctx.HandValue == 10 && dealerValue <= 9) return PlayerAction.DoubleDown;
            
            // Double on 9 if dealer shows 3-6
            if (ctx.HandValue == 9 && dealerValue >= 3 && dealerValue <= 6) return PlayerAction.DoubleDown;
        }
        
        // Soft hands (with Ace counted as 11)
        if (ctx.IsSoft)
        {
            // Soft 19+ always stand
            if (ctx.HandValue >= 19) return PlayerAction.Stand;
            
            // Soft 18: stand vs dealer 2-8, hit vs 9-A
            if (ctx.HandValue == 18)
                return dealerValue >= 9 ? PlayerAction.Hit : PlayerAction.Stand;
            
            // Soft 17 or less: always hit
            return PlayerAction.Hit;
        }
        
        // Hard hands
        // Always stand on 17+
        if (ctx.HandValue >= 17) return PlayerAction.Stand;
        
        // Always hit on 11 or less
        if (ctx.HandValue <= 11) return PlayerAction.Hit;
        
        // 12-16: depends on dealer card
        if (ctx.HandValue >= 12 && ctx.HandValue <= 16)
        {
            // Stand if dealer shows 2-6 (dealer bust territory)
            if (dealerValue >= 2 && dealerValue <= 6) return PlayerAction.Stand;
            
            // Hit if dealer shows 7+ (dealer likely to make hand)
            return PlayerAction.Hit;
        }
        
        // Default: stand
        return PlayerAction.Stand;
    }

    private string GetReasoning(ActionContext ctx, PlayerAction action)
    {
        var dealerValue = GetDealerUpCardValue(ctx.DealerUpCard);
        
        return action switch
        {
            PlayerAction.DoubleDown when ctx.HandValue == 11 
                => "Basic strategy: Always double on 11",
            
            PlayerAction.DoubleDown when ctx.HandValue == 10 
                => $"Basic strategy: Double on 10 vs dealer {dealerValue}",
            
            PlayerAction.DoubleDown 
                => $"Basic strategy: Double on {ctx.HandValue} vs dealer {dealerValue}",
            
            PlayerAction.Hit when ctx.IsSoft 
                => $"Basic strategy: Hit soft {ctx.HandValue}",
            
            PlayerAction.Hit when ctx.HandValue <= 11 
                => "Basic strategy: Always hit 11 or less",
            
            PlayerAction.Hit when dealerValue >= 7 
                => $"Basic strategy: Hit {ctx.HandValue} vs dealer {dealerValue}+",
            
            PlayerAction.Stand when ctx.IsSoft && ctx.HandValue >= 19 
                => "Basic strategy: Stand on soft 19+",
            
            PlayerAction.Stand when ctx.HandValue >= 17 
                => $"Basic strategy: Stand on hard {ctx.HandValue}",
            
            PlayerAction.Stand when dealerValue <= 6 
                => $"Basic strategy: Stand on {ctx.HandValue} vs dealer bust card",
            
            _ => $"Basic strategy: {action} on {ctx.HandValue}"
        };
    }

    private int GetDealerUpCardValue(Card card)
    {
        return card.Rank switch
        {
            Rank.Ace => 11,
            Rank.King or Rank.Queen or Rank.Jack => 10,
            _ => (int)card.Rank
        };
    }
}