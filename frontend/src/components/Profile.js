import React, { useState, useEffect } from 'react';
import './Profile.css';

const Profile = ({ user, onBack }) => {
  const [profileData, setProfileData] = useState({
    username: user?.username || '',
    playerId: user?.playerId || '',
    joinDate: '',
    totalGames: 0,
    recentGames: [],
    personalRecords: {
      highestScore: 0,
      bestAccuracy: 0,
      longestStreak: 0,
      fastestGame: null
    }
  });
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchProfileData = async () => {
      try {
        setLoading(true);
        
        // Fetch player statistics
        const statsResponse = await fetch('http://localhost:5216/statistics/player', {
          headers: {
            'Authorization': `Bearer ${localStorage.getItem('token')}`
          }
        });

        if (statsResponse.ok) {
          const stats = await statsResponse.json();
          
          // For now, we'll create mock recent games and records
          // In a real app, you'd fetch this from your backend
          const mockRecentGames = [
            { id: 1, category: 'Science', score: 85, accuracy: 85, date: '2024-01-15', duration: '2:30' },
            { id: 2, category: 'History', score: 92, accuracy: 92, date: '2024-01-14', duration: '3:15' },
            { id: 3, category: 'Geography', score: 78, accuracy: 78, date: '2024-01-13', duration: '2:45' },
            { id: 4, category: 'Science', score: 95, accuracy: 95, date: '2024-01-12', duration: '2:10' },
            { id: 5, category: 'Movies', score: 88, accuracy: 88, date: '2024-01-11', duration: '3:05' }
          ];

          const personalRecords = {
            highestScore: stats.bestScore || 0,
            bestAccuracy: stats.totalQuestionsAnswered > 0 ? 
              Math.round((stats.totalCorrectAnswers / stats.totalQuestionsAnswered) * 100) : 0,
            longestStreak: Math.floor(Math.random() * 10) + 1, // Mock data
            fastestGame: '1:45' // Mock data
          };

          setProfileData({
            username: user?.username || '',
            playerId: user?.playerId || '',
            joinDate: '2024-01-01', // Mock join date
            totalGames: stats.totalGamesPlayed || 0,
            recentGames: mockRecentGames,
            personalRecords
          });
        }
      } catch (error) {
        console.error('Error fetching profile data:', error);
      } finally {
        setLoading(false);
      }
    };

    fetchProfileData();
  }, [user]);

  const formatDate = (dateString) => {
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  };

  if (loading) {
    return (
      <div className="profile-container">
        <div className="profile-card">
          <div className="loading-spinner"></div>
          <p>Loading profile...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="profile-container">
      <div className="profile-card">
        {/* User Identity Section */}
        <div className="profile-header">
          <div className="user-identity">
            <div className="user-avatar-large">
              {profileData.username?.charAt(0)?.toUpperCase() || 'U'}
            </div>
            <div className="user-info">
              <h1 className="username-display">{profileData.username}</h1>
              <div className="user-meta">
                <span className="player-id">ID: {profileData.playerId}</span>
                <span className="join-date">Joined {formatDate(profileData.joinDate)}</span>
              </div>
            </div>
          </div>
          <div className="quick-stats">
            <div className="quick-stat">
              <div className="stat-number">{profileData.totalGames}</div>
              <div className="stat-label">Games Played</div>
            </div>
          </div>
        </div>

        {/* Personal Records Section */}
        <div className="records-section">
          <h2 className="section-title">Personal Records</h2>
          <div className="records-grid">
            <div className="record-card">
              <div className="record-icon">🏆</div>
              <div className="record-content">
                <div className="record-value">{profileData.personalRecords.highestScore}%</div>
                <div className="record-label">Highest Score</div>
              </div>
            </div>
            <div className="record-card">
              <div className="record-icon">🎯</div>
              <div className="record-content">
                <div className="record-value">{profileData.personalRecords.bestAccuracy}%</div>
                <div className="record-label">Best Accuracy</div>
              </div>
            </div>
            <div className="record-card">
              <div className="record-icon">🔥</div>
              <div className="record-content">
                <div className="record-value">{profileData.personalRecords.longestStreak}</div>
                <div className="record-label">Win Streak</div>
              </div>
            </div>
            <div className="record-card">
              <div className="record-icon">⚡</div>
              <div className="record-content">
                <div className="record-value">{profileData.personalRecords.fastestGame}</div>
                <div className="record-label">Fastest Game</div>
              </div>
            </div>
          </div>
        </div>

        {/* Game History Section */}
        <div className="history-section">
          <h2 className="section-title">Recent Games</h2>
          <div className="games-list">
            {profileData.recentGames.length === 0 ? (
              <div className="no-games">
                <p>No games played yet</p>
              </div>
            ) : (
              profileData.recentGames.map((game) => (
                <div key={game.id} className="game-item">
                  <div className="game-category">
                    <span className="category-badge">{game.category}</span>
                  </div>
                  <div className="game-stats">
                    <div className="game-score">
                      <span className="score-value">{game.score}%</span>
                      <span className="score-label">Score</span>
                    </div>
                    <div className="game-accuracy">
                      <span className="accuracy-value">{game.accuracy}%</span>
                      <span className="accuracy-label">Accuracy</span>
                    </div>
                    <div className="game-duration">
                      <span className="duration-value">{game.duration}</span>
                      <span className="duration-label">Time</span>
                    </div>
                  </div>
                  <div className="game-date">
                    {formatDate(game.date)}
                  </div>
                </div>
              ))
            )}
          </div>
        </div>

        <button className="back-button" onClick={onBack}>
          <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor">
            <path d="M8 0L0 8l8 8V0z"/>
          </svg>
          Back
        </button>
      </div>
    </div>
  );
};

export default Profile;
