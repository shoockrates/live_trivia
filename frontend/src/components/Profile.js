import React, { useState, useEffect } from 'react';
import './Profile.css';

const Profile = ({ user, onBack }) => {
    const [stats, setStats] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState('');
    const API_BASE = '/api';

    useEffect(() => {
        fetchProfileData();
    }, [user]);

    const fetchProfileData = async () => {
        try {
            setLoading(true);
            setError('');

            const response = await fetch(`${API_BASE}/statistics/player`, {
                headers: {
                    'Authorization': `Bearer ${localStorage.getItem('token')}`,
                    'Content-Type': 'application/json'
                }
            });

            if (response.ok) {
                const data = await response.json();
                setStats(data);
            } else {
                setError('Failed to load profile data');
            }
        } catch (error) {
            console.error('Error fetching profile:', error);
            setError('Error connecting to server');
        } finally {
            setLoading(false);
        }
    };

    const getMostPlayedCategory = () => {
        if (!stats?.categoryStats || stats.categoryStats.length === 0) return null;
        return stats.categoryStats.reduce((prev, current) =>
            current.gamesPlayed > prev.gamesPlayed ? current : prev
        );
    };

    const getBestCategory = () => {
        if (!stats?.categoryStats || stats.categoryStats.length === 0) return null;
        return stats.categoryStats.reduce((prev, current) =>
            current.accuracy > prev.accuracy ? current : prev
        );
    };

    const formatDate = (dateString) => {
        if (!dateString) return 'Never';
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

    if (error) {
        return (
            <div className="profile-container">
                <div className="profile-card">
                    <div className="error-message">{error}</div>
                    <button className="back-button" onClick={onBack}>Back</button>
                </div>
            </div>
        );
    }

    const mostPlayed = getMostPlayedCategory();
    const bestCategory = getBestCategory();

    return (
        <div className="profile-container">
            <div className="profile-card">
                {/* Header Section */}
                <div className="profile-header">
                    <div className="profile-avatar-large">
                        {user?.username?.charAt(0)?.toUpperCase() || 'U'}
                    </div>
                    <div className="profile-user-info">
                        <h1 className="profile-username">{user?.username}</h1>
                        <div className="profile-meta">
                            <span className="profile-id">ID: {user?.playerId}</span>
                        </div>
                    </div>
                </div>

                {/* Quick Stats Grid */}
                <div className="stats-grid">
                    <div className="stat-box">
                        <div className="stat-icon">🎮</div>
                        <div className="stat-content">
                            <div className="stat-value">{stats?.totalGamesPlayed || 0}</div>
                            <div className="stat-label">Games Played</div>
                        </div>
                    </div>
                    <div className="stat-box">
                        <div className="stat-icon">🎯</div>
                        <div className="stat-content">
                            <div className="stat-value">{stats?.accuracyPercentage?.toFixed(1) || 0}%</div>
                            <div className="stat-label">Accuracy</div>
                        </div>
                    </div>
                    <div className="stat-box">
                        <div className="stat-icon">⭐</div>
                        <div className="stat-content">
                            <div className="stat-value">{stats?.bestScore || 0}</div>
                            <div className="stat-label">Best Score</div>
                        </div>
                    </div>
                    <div className="stat-box">
                        <div className="stat-icon">📊</div>
                        <div className="stat-content">
                            <div className="stat-value">{stats?.averageScore?.toFixed(1) || 0}</div>
                            <div className="stat-label">Avg Score</div>
                        </div>
                    </div>
                </div>

                {/* Performance Overview */}
                <div className="section">
                    <h2 className="section-title">Performance Overview</h2>
                    <div className="performance-details">
                        <div className="detail-row">
                            <span className="detail-label">Total Questions Answered</span>
                            <span className="detail-value">{stats?.totalQuestionsAnswered || 0}</span>
                        </div>
                        <div className="detail-row">
                            <span className="detail-label">Correct Answers</span>
                            <span className="detail-value success">{stats?.totalCorrectAnswers || 0}</span>
                        </div>
                        <div className="detail-row">
                            <span className="detail-label">Cumulative Score</span>
                            <span className="detail-value">{stats?.totalScore?.toLocaleString() || 0} pts</span>
                        </div>
                        <div className="detail-row">
                            <span className="detail-label">Last Played</span>
                            <span className="detail-value">{formatDate(stats?.lastPlayedAt)}</span>
                        </div>
                    </div>
                </div>

                {/* Highlights */}
                {(mostPlayed || bestCategory) && (
                    <div className="section">
                        <h2 className="section-title">Highlights</h2>
                        <div className="highlights-grid">
                            {mostPlayed && (
                                <div className="highlight-card">
                                    <div className="highlight-icon">🔥</div>
                                    <div className="highlight-content">
                                        <div className="highlight-title">Most Played</div>
                                        <div className="highlight-value">{mostPlayed.category}</div>
                                        <div className="highlight-subtitle">{mostPlayed.gamesPlayed} games</div>
                                    </div>
                                </div>
                            )}
                            {bestCategory && (
                                <div className="highlight-card">
                                    <div className="highlight-icon">🏆</div>
                                    <div className="highlight-content">
                                        <div className="highlight-title">Best Category</div>
                                        <div className="highlight-value">{bestCategory.category}</div>
                                        <div className="highlight-subtitle">{bestCategory.accuracy.toFixed(1)}% accuracy</div>
                                    </div>
                                </div>
                            )}
                        </div>
                    </div>
                )}

                {/* Category Breakdown */}
                {stats?.categoryStats && stats.categoryStats.length > 0 && (
                    <div className="section">
                        <h2 className="section-title">Category Performance</h2>
                        <div className="category-list">
                            {stats.categoryStats
                                .sort((a, b) => b.gamesPlayed - a.gamesPlayed)
                                .map((cat) => (
                                    <div key={cat.category} className="category-item">
                                        <div className="category-header">
                                            <span className="category-name">{cat.category}</span>
                                            <span className="category-games">{cat.gamesPlayed} games</span>
                                        </div>
                                        <div className="category-bar">
                                            <div
                                                className="category-fill"
                                                style={{ width: `${cat.accuracy}%` }}
                                            >
                                                <span className="category-percentage">{cat.accuracy.toFixed(1)}%</span>
                                            </div>
                                        </div>
                                        <div className="category-details">
                                            <span>{cat.correctAnswers} / {cat.totalQuestions} correct</span>
                                        </div>
                                    </div>
                                ))}
                        </div>
                    </div>
                )}

                {/* No Stats Message */}
                {(!stats || stats.totalGamesPlayed === 0) && (
                    <div className="no-stats">
                        <div className="no-stats-icon">🎲</div>
                        <h3>No Games Played Yet</h3>
                        <p>Start playing to see your statistics!</p>
                    </div>
                )}

                <button className="back-button" onClick={onBack}>
                    <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor">
                        <path d="M8 0L0 8l8 8V0z" />
                    </svg>
                    Back
                </button>
            </div>
        </div>
    );
};

export default Profile;
