import React from 'react';
import './MultiplayerResults.css';
import signalRService from '../services/signalRService.js';

const MultiplayerResults = ({
    finalResults,
    onBackToLobby,
    onPlayAgain,
    roomCode,
    isHost
}) => {
    const sortedPlayers = [...finalResults.players].sort((a, b) => b.score - a.score);
    const [isResetting, setIsResetting] = React.useState(false);

    const handlePlayAgain = async () => {
        if (!isHost) {
            alert('Only the host can start a new game');
            return;
        }

        if (isResetting) {
            console.log('MultiplayerResults: Reset already in progress, ignoring duplicate click');
            return;
        }

        setIsResetting(true);
        console.log('MultiplayerResults: Play Again clicked, calling reset endpoint...');

        const connectionState = signalRService.getConnectionState();
        console.log('MultiplayerResults: SignalR connection state:', connectionState);

        try {
            const response = await fetch(`/api/games/${roomCode}/reset`, {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${localStorage.getItem('token')}`
                }
            });

            if (!response.ok) {
                const errorText = await response.text();
                console.error('Reset failed:', errorText);
                setIsResetting(false);
                throw new Error('Failed to reset game');
            }

            const resetData = await response.json();
            console.log('MultiplayerResults: Game reset successfully, response:', resetData);
            console.log('MultiplayerResults: Waiting for GameReset SignalR event to redirect players...');

        } catch (error) {
            console.error('MultiplayerResults: Failed to reset game:', error);
            setIsResetting(false);
            alert('Failed to start new game. Please try again.');
        }
    };

    return (
        <div className="multiplayer-results-container">
            <div className="results-card">
                <div className="results-header">
                    <h1 className="results-title">Game Over!</h1>
                    <p className="room-code-info">Room: <strong>{roomCode}</strong></p>
                </div>

                <div className="podium">
                    {sortedPlayers[0] && (
                        <div className="podium-place first">
                            <div className="medal">🥇</div>
                            <div className="player-name">{sortedPlayers[0].name}</div>
                            <div className="player-score">{sortedPlayers[0].score} pts</div>
                        </div>
                    )}
                    {sortedPlayers[1] && (
                        <div className="podium-place second">
                            <div className="medal">🥈</div>
                            <div className="player-name">{sortedPlayers[1].name}</div>
                            <div className="player-score">{sortedPlayers[1].score} pts</div>
                        </div>
                    )}
                    {sortedPlayers[2] && (
                        <div className="podium-place third">
                            <div className="medal">🥉</div>
                            <div className="player-name">{sortedPlayers[2].name}</div>
                            <div className="player-score">{sortedPlayers[2].score} pts</div>
                        </div>
                    )}
                </div>

                <div className="full-leaderboard">
                    <h3>Final Standings</h3>
                    <div className="leaderboard-list">
                        {sortedPlayers.map((player, index) => (
                            <div key={player.playerId} className={`leaderboard-item ${index < 3 ? 'top3' : ''}`}>
                                <div className="rank">#{index + 1}</div>
                                <div className="player-info">
                                    <div className="player-avatar">
                                        {player.name.charAt(0).toUpperCase()}
                                    </div>
                                    <div>
                                        <div className="player-name">{player.name}</div>
                                        <div className="player-stats">
                                            {player.correct} correct • {player.wrong} wrong
                                        </div>
                                    </div>
                                </div>
                                <div className="final-score">{player.score} pts</div>
                            </div>
                        ))}
                    </div>
                </div>

                <div className="results-actions">
                    <button className="action-button secondary" onClick={onBackToLobby}>
                        Back to Lobby
                    </button>
                    {isHost ? (
                        <button
                            className="action-button primary"
                            onClick={handlePlayAgain}
                            disabled={isResetting}
                        >
                            {isResetting ? (
                                <>
                                    <div className="waiting-spinner"></div>
                                    <span>Resetting...</span>
                                </>
                            ) : (
                                <span>Play Again</span>
                            )}
                        </button>
                    ) : (
                        <div className="action-button waiting-host">
                            <div className="waiting-spinner"></div>
                            <span>Waiting for host...</span>
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
};

export default MultiplayerResults;
