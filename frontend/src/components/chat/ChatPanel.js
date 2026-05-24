import React, { useEffect, useRef, useState } from 'react';
import signalRService from '../../services/signalRService';
import './ChatPanel.css';

const ALLOWED_EMOJIS = ['👍', '❤️', '😂', '😮', '😢', '🔥'];

const ChatPanel = ({ roomId, currentPlayerId, disabled = false, readOnly = false }) => {
    const [messages, setMessages] = useState([]);
    const [text, setText] = useState('');
    const [error, setError] = useState('');
    const bottomRef = useRef(null);

    useEffect(() => {
        if (!roomId) return;

        const handleHistoryLoaded = (history) => {
            setMessages(Array.isArray(history) ? history : []);
        };

        const handleMessageReceived = (message) => {
            setMessages((prev) => {
                if (prev.some((m) => m.id === message.id)) return prev;
                return [...prev, message];
            });
        };

        const handleReactionUpdated = (updatedMessage) => {
            setMessages((prev) =>
                prev.map((m) => (m.id === updatedMessage.id ? updatedMessage : m))
            );
        };

        const handleMessageDeleted = (data) => {
            const deletedId = data.messageId ?? data.MessageId;
            setMessages((prev) => prev.filter((m) => m.id !== deletedId));
        };

        const handleChatError = (message) => {
            setError(message);
            setTimeout(() => setError(''), 3500);
        };

        signalRService.onChatHistoryLoaded(handleHistoryLoaded);
        signalRService.onChatMessageReceived(handleMessageReceived);
        signalRService.onChatReactionUpdated(handleReactionUpdated);
        signalRService.onChatMessageDeleted(handleMessageDeleted);
        signalRService.onChatError(handleChatError);

        signalRService.getChatHistory(roomId).catch((err) => {
            setError(err.message || 'Failed to load chat history');
        });

        return () => {
            signalRService.removeListener('ChatHistoryLoaded', handleHistoryLoaded);
            signalRService.removeListener('ChatMessageReceived', handleMessageReceived);
            signalRService.removeListener('ChatReactionUpdated', handleReactionUpdated);
            signalRService.removeListener('ChatMessageDeleted', handleMessageDeleted);
            signalRService.removeListener('ChatError', handleChatError);
        };
    }, [roomId]);

    useEffect(() => {
        bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, [messages]);

    const handleSend = async (event) => {
        event.preventDefault();

        const trimmed = text.trim();

        if (!trimmed) return;
        if (disabled || readOnly) return;

        try {
            await signalRService.sendChatMessage(roomId, trimmed);
            setText('');
        } catch (err) {
            setError(err.message || 'Failed to send message');
        }
    };

    const handleReact = async (messageId, emoji) => {
        if (disabled || readOnly) return;

        try {
            await signalRService.reactToMessage(messageId, emoji);
        } catch (err) {
            setError(err.message || 'Failed to react');
        }
    };

    const handleDelete = async (messageId) => {
        if (disabled || readOnly) return;

        const confirmed = window.confirm('Delete this message?');
        if (!confirmed) return;

        try {
            await signalRService.deleteChatMessage(messageId);
        } catch (err) {
            setError(err.message || 'Failed to delete message');
        }
    };

    return (
        <div className="chat-panel">
            <div className="chat-header">
                <div>
                    <h3>Room Chat</h3>
                    <p>
                        {readOnly
                            ? 'History view'
                            : disabled
                                ? 'Chat disabled during game'
                                : 'Talk with players in this room'}
                    </p>
                </div>
            </div>

            {error && <div className="chat-error">{error}</div>}

            <div className="chat-messages">
                {messages.length === 0 ? (
                    <div className="chat-empty">No messages yet.</div>
                ) : (
                    messages.map((message) => {
                        const isMine =
                            Number(message.senderPlayerId) === Number(currentPlayerId);

                        return (
                            <div
                                key={message.id}
                                className={[
                                    'chat-message-row',
                                    message.isSystemMessage
                                        ? 'system'
                                        : isMine
                                            ? 'mine'
                                            : 'other'
                                ].join(' ')}
                            >
                                <div className="chat-message">
                                    {!message.isSystemMessage && (
                                        <div className="chat-message-meta">
                                            <span>{isMine ? 'You' : message.senderName}</span>
                                            <span>
                                                {new Date(message.sentAt).toLocaleTimeString([], {
                                                    hour: '2-digit',
                                                    minute: '2-digit'
                                                })}
                                            </span>
                                        </div>
                                    )}

                                    <div className="chat-message-text">
                                        {message.message}
                                    </div>

                                    {!message.isSystemMessage && (
                                        <div className="chat-actions">
                                            {!readOnly && !disabled && (
                                                <div className="emoji-actions">
                                                    {ALLOWED_EMOJIS.map((emoji) => (
                                                        <button
                                                            key={emoji}
                                                            type="button"
                                                            onClick={() => handleReact(message.id, emoji)}
                                                        >
                                                            {emoji}
                                                        </button>
                                                    ))}
                                                </div>
                                            )}

                                            {isMine && !readOnly && !disabled && (
                                                <button
                                                    type="button"
                                                    className="delete-message-button"
                                                    onClick={() => handleDelete(message.id)}
                                                >
                                                    Delete
                                                </button>
                                            )}
                                        </div>
                                    )}

                                    {message.reactions && Object.keys(message.reactions).length > 0 && (
                                        <div className="reaction-list">
                                            {Object.entries(message.reactions).map(([emoji, count]) => (
                                                <span key={emoji} className="reaction-pill">
                                                    {emoji} {count}
                                                </span>
                                            ))}
                                        </div>
                                    )}
                                </div>
                            </div>
                        );
                    })
                )}

                <div ref={bottomRef} />
            </div>

            {!readOnly && !disabled && (
                <form className="chat-input-form" onSubmit={handleSend}>
                    <input
                        value={text}
                        onChange={(e) => setText(e.target.value)}
                        placeholder="Type a message..."
                        maxLength={500}
                    />
                    <button type="submit" disabled={!text.trim()}>
                        Send
                    </button>
                </form>
            )}
        </div>
    );
};

export default ChatPanel;