using System.Text.Json;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

public class AIPlayerStrategy : IPlayerStrategy
{
    private readonly string _model;
    private readonly ChatClient _client;

    public AIPlayerStrategy(string model, string apiKey, string endpoint)
    {
        _model = model;

        _client = new ChatClient(
            model: _model,
            credential: new ApiKeyCredential(apiKey),
            options: new OpenAIClientOptions()
            {
                Endpoint = new Uri(endpoint),
            }
        );
    }

    public async Task<BetDecision> DecideBetAsync(BetContext context)
    {
        var prompt = BuildBetPrompt(context);
        var response = await CallLLMAsync(prompt);
        return ParseBetResponse(response, context);
    }

    public async Task<ActionDecision> DecideActionAsync(ActionContext context)
    {
        var prompt = BuildActionPrompt(context);
        var response = await CallLLMAsync(prompt);
        return ParseActionResponse(response, context);
    }

    private string BuildBetPrompt(BetContext ctx)
    {
        var opponents = ctx.Opponents.Count > 0 ? string.Join("\n", ctx.Opponents.Select(o => o.CurrentBet > 0 ? $"  - {o.Name}: Balance ${o.Balance}, Bet ${o.CurrentBet}" : $"  - {o.Name}: Balance ${o.Balance} (not yet bet)")) : "  (none)";

        return $$"""
            You are playing blackjack at a casino. Your name is {{ctx.PlayerName}}.
            
            YOUR STATUS:
            - Balance: ${{ctx.Balance}}
            - Minimum bet: ${{ctx.MinBet}}
            
            OTHER PLAYERS AT THE TABLE:
            {{opponents}}
            
            Decide how much to bet (must be between ${{ctx.MinBet}} and ${{ctx.Balance}}).
            Consider your position relative to other players and how much risk you want to take.
            
            Respond with ONLY valid JSON in this exact format:
            {"amount": <number>, "reasoning": "<your strategic thinking in 1-2 sentences>"}
            """;
    }

    private string BuildActionPrompt(ActionContext ctx)
    {
        var historySection = BuildHistorySection(ctx.ActionHistory);
        var otherPlayersSection = BuildOtherPlayersSection(ctx.OtherPlayers);
        var actionsAvailable = ctx.CanDoubleDown ? "Hit, Stand, or DoubleDown" : "Hit or Stand";

        return $$"""
            You are playing blackjack. Your name is {{ctx.PlayerName}}.
            You and other AI players are playing together during a relaxing casino night.
            Make strategic decisions based on your hand, the dealer's up card, and other players' visible hands.
            
            DEALER SHOWS: {{ctx.DealerUpCard}}
            
            {{historySection}}
            
            YOUR CURRENT HAND: {{ctx.HandDescription}}
            - Value: {{ctx.HandValue}}{{(ctx.IsSoft ? " (soft)" : "")}}
            - Current bet: ${{ctx.CurrentBet}}
            - Remaining balance: ${{ctx.Balance}}
            
            {{otherPlayersSection}}
            
            AVAILABLE ACTIONS: {{actionsAvailable}}
            {{(ctx.CanDoubleDown ? "Note: DoubleDown doubles your bet and you receive exactly one more card." : "")}}
            
            Respond with ONLY valid JSON in this exact format:
            {"action": "Hit" or "Stand" or "DoubleDown", "reasoning": "<your strategic thinking in 1-2 sentences>"}
            """;
    }

    private string BuildHistorySection(IReadOnlyList<ActionHistoryEntry> history)
    {
        if (history.Count == 0)
            return "THIS IS YOUR FIRST DECISION THIS HAND.";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("YOUR DECISIONS THIS HAND:");
        
        foreach (var entry in history)
        {
            sb.AppendLine($"  - Hand was {entry.HandDescription} (value {entry.HandValue})");
            sb.AppendLine($"    You chose: {entry.ActionTaken}");
            sb.AppendLine($"    Reasoning: \"{entry.Reasoning}\"");
            if (entry.CardReceived.HasValue)
                sb.AppendLine($"    Card received: {entry.CardReceived.Value}");
        }

        // You know I vibecoded this part of the application because I rarely use StringBuilder.
        // But hey, at least you know I read though the code so I know what's in it.
        return sb.ToString();
    }

    private string BuildOtherPlayersSection(IReadOnlyList<OpponentHandInfo> others)
    {
        if (others.Count == 0)
            return "OTHER PLAYERS: (none)";

        var lines = others.Select(p =>
            $"  - {p.Name}: {p.HandDescription} (value {p.HandValue}){(p.IsBust ? " BUST" : "")}");

        return "OTHER PLAYERS' VISIBLE HANDS:\n" + string.Join("\n", lines);
    }

    private async Task<string> CallLLMAsync(string prompt)
    {
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage("You are a professional and strategic blackjack player. Be concise. Always respond with valid JSON only, no markdown."),
            new UserChatMessage(prompt)
        };

        var chatOptions = new ChatCompletionOptions
        {
            Temperature = 0.7f,
        };

        try
        {
            // Since I'm using OpenRouter, sometimes things don't come back cleanly.
            ChatCompletion completion = await _client.CompleteChatAsync(messages, chatOptions);
            if (completion.Usage is not null)
            {
                UsageTracker.AddUsage(_model, completion.Usage.InputTokenCount, completion.Usage.OutputTokenCount, completion.Usage.TotalTokenCount);
            }
        
            return completion.Content[0].Text;
        }
        catch (ArgumentOutOfRangeException ex) when (ex.Message.Contains("Unknown ChatFinishReason"))
        {
            Console.WriteLine($"[Warning] OpenAI SDK could not parse 'finish_reason'. Using fallback. Error: {ex.Message}");
            
            // Try to extract the actual finish_reason value from the exception
            try
            {
                var paramMatch = System.Text.RegularExpressions.Regex.Match(ex.Message, "Actual value was\\s+['\\\"]?([^'\\\"]+)['\\\"]?");
                if (!paramMatch.Success)
                {
                    paramMatch = System.Text.RegularExpressions.Regex.Match(ex.Message, "value[:\\s]+['\\\"]?([^'\\\"\\.]+)['\\\"]?");
                }
                
                if (paramMatch.Success)
                {
                    Console.WriteLine($"[Debug] Raw finish_reason value: '{paramMatch.Groups[1].Value}'");
                }
                else
                {
                    Console.WriteLine($"[Debug] Could not extract finish_reason value. Full exception: {ex}");
                }
                
                // Also try reflection to get more details
                var stackTrace = ex.StackTrace;
                Console.WriteLine($"[Debug] Stack trace: {stackTrace}");
            }
            catch (Exception debugEx)
            {
                Console.WriteLine($"[Debug] Error while debugging finish_reason: {debugEx.Message}");
            }
            
            // Return a safe default response or retry logic here
            // For Blackjack, asking for a generic valid move might be safer, or just returning "Stand" if critical failure
            return "Stand"; 
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error calling LLM: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return "Stand"; // Safe fallback
        }
        
    }

    private BetDecision ParseBetResponse(string response, BetContext ctx)
    {
        try
        {
            response = CleanJsonResponse(response);
            var json = JsonSerializer.Deserialize<JsonElement>(response);
            var amount = json.GetProperty("amount").GetInt32();
            var reasoning = json.GetProperty("reasoning").GetString() ?? "No reasoning provided.";

            amount = Math.Clamp(amount, ctx.MinBet, ctx.Balance);
            return new BetDecision(amount, reasoning);
        }
        catch
        {
            return new BetDecision(ctx.MinBet, $"[Parse error, defaulting to min bet] Raw: {response}");
        }
    }

    private ActionDecision ParseActionResponse(string response, ActionContext ctx)
    {
        try
        {
            response = CleanJsonResponse(response);
            var json = JsonSerializer.Deserialize<JsonElement>(response);
            var actionStr = json.GetProperty("action").GetString() ?? "Stand";
            var reasoning = json.GetProperty("reasoning").GetString() ?? "No reasoning provided.";

            var action = actionStr.ToLowerInvariant() switch
            {
                "hit" => PlayerAction.Hit,
                "doubledown" => PlayerAction.DoubleDown,
                _ => PlayerAction.Stand
            };

            // Prevent invalid double down
            if (action == PlayerAction.DoubleDown && !ctx.CanDoubleDown)
                action = PlayerAction.Hit;

            return new ActionDecision(action, reasoning);
        }
        catch
        {
            return new ActionDecision(PlayerAction.Stand, $"[Parse error, defaulting to stand] Raw: {response}");
        }
    }

    private static string CleanJsonResponse(string response)
    {
        response = response.Trim();
        if (response.StartsWith("```json")) response = response[7..];
        if (response.StartsWith("```")) response = response[3..];
        if (response.EndsWith("```")) response = response[..^3];
        return response.Trim();
    }
}