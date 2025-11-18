import React from 'react';
import './MultiplayerResults.css';

const MultiplayerResults = ({
    finalResults,
    onBackToLobby,
    onPlayAgain,
    roomCode
}) => {
    const sortedPlayers = [...finalResults.players].sort((a, b) => b.score - a.score);

    return (
        <div className="multiplayer-results-container">
            <div className="results-card">
                <div className="results-header">
                    <h1>Game Over!</h1>
                    <p className="room-code-info">Room: <strong>{roomCode}</strong></p>
                </div>

                <div className="podium">
                    {sortedPlayers[0] && (
                        <div className="podium-place first">
                            <div className="medal">1st</div>
                            <div className="player-name">{sortedPlayers[0].name}</div>
                            <div className="player-score">{sortedPlayers[0].score} pts</div>
                        </div>
                    )}
                    {sortedPlayers[1] && (
                        <div className="podium-place second">
                            <div className="medal">2nd</div>
                            <div className="player-name">{sortedPlayers[1].name}</div>
                            <div className="player-score">{sortedPlayers[1].score} pts</div>
                        </div>
                    )}
                    {sortedPlayers[2] && (
                        <div className="podium-place third">
                            <div className="medal">3rd</div>
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
                    <button className="action-button primary" onClick={onPlayAgain}>
                        Play Again
                    </button>
                </div>
            </div>
        </div>
    );
};

export default MultiplayerResults;
