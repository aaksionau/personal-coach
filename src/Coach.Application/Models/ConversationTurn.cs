using Coach.Domain.Entities;

namespace Coach.Application.Models;

/// <summary>The persisted user/assistant message pair produced by one chat turn.</summary>
public sealed record ConversationTurn(ChatMessage UserMessage, ChatMessage AssistantMessage);
