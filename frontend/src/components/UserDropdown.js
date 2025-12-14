import React, { useState, useRef, useEffect } from 'react';
import './UserDropdown.css';
const API_BASE = "http://localhost:5216";
const UserDropdown = ({ user, onLogout, onShowStats, onShowLeaderboard, onShowProfile }) => {
    const [isOpen, setIsOpen] = useState(false);
    const dropdownRef = useRef(null);
    const fileInputRef = useRef(null);


    useEffect(() => {
        const handleClickOutside = (event) => {
            if (dropdownRef.current && !dropdownRef.current.contains(event.target)) {
                setIsOpen(false);
            }
        };

        // Only add listener when dropdown is open
        if (isOpen) {
            // Use setTimeout to avoid the React event system conflict
            const timer = setTimeout(() => {
                document.addEventListener('click', handleClickOutside);
            }, 0);

            return () => {
                clearTimeout(timer);
                document.removeEventListener('click', handleClickOutside);
            };
        }
    }, [isOpen]); // Only re-run when isOpen changes

    const toggleDropdown = (e) => {
        e.stopPropagation();
        setIsOpen(!isOpen);
    };

    const handleLogout = () => {
        onLogout();
        setIsOpen(false);
    };


    function parseJwt(token) {
        if (!token) return null;
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        return JSON.parse(atob(base64));
    }

    const token = localStorage.getItem("token");
    const claims = parseJwt(token);

    const role =
        claims?.role ??
        claims?.["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];

    const isAdmin = role === "Admin";

    // Generate user initial for avatar
    const getUserInitial = () => {
        return user?.username?.charAt(0)?.toUpperCase() || 'U';
    };

    // Handle item clicks
    const handleItemClick = (action) => {
        console.log(`Clicked: ${action}`);
        setIsOpen(false);

        if (action === 'statistics' && onShowStats) {
            onShowStats();
        } else if (action === 'history' && onShowLeaderboard) {
            onShowLeaderboard();
        } else if (action === 'profile' && onShowProfile) {
            onShowProfile();
        }
    };

    const handleExportQuestions = () => {
        fetch(`${API_BASE}/questions/export`, {
            method: "GET",
            headers: {
                Authorization: `Bearer ${token}`,
            },
        })
            .then(res => {
                if (!res.ok) throw new Error("Export failed");
                return res.blob();
            })
            .then(blob => {
                const url = window.URL.createObjectURL(blob);
                const a = document.createElement("a");
                a.href = url;
                a.download = "question-bank.json";
                a.click();
                window.URL.revokeObjectURL(url);
            })
            .catch(err => {
                console.error(err);
                alert("Failed to export questions");
            });
    };

    const handleImportQuestions = async (e) => {
        const file = e.target.files[0];
        if (!file) return;

        const formData = new FormData();
        formData.append("file", file);

        try {
            const res = await fetch(`${API_BASE}/questions/import`, {
                method: "POST",
                headers: {
                    Authorization: `Bearer ${token}`,
                },
                body: formData,
            });

            if (!res.ok) throw new Error("Import failed");

            const result = await res.json();
            alert(
                `Import complete:
                Added: ${result.added}
                Duplicates: ${result.skippedDuplicates}
                Invalid: ${result.skippedInvalid}`
            );
        } catch (err) {
            console.error(err);
            alert("Failed to import questions");
        } finally {
            e.target.value = "";
        }
    };

    return (
        <div className="user-dropdown" ref={dropdownRef}>
            <button
                className="user-dropdown-toggle"
                onClick={toggleDropdown}
                aria-label="User menu"
                aria-expanded={isOpen}
                type="button"
            >
                <div className="user-avatar">
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor">
                        <path d="M12 0c-6.627 0-12 5.373-12 12s5.373 12 12 12 12-5.373 12-12-5.373-12-12-12zm0 22c-3.123 0-5.914-1.441-7.749-3.69.259-.588.783-.995 1.867-1.246 2.244-.518 4.459-.981 3.393-2.945-3.155-5.82-.899-9.119 2.489-9.119 3.322 0 5.634 3.177 2.489 9.119-1.035 1.952 1.1 2.416 3.393 2.945 1.082.25 1.61.655 1.871 1.241-1.836 2.253-4.628 3.695-7.753 3.695z" />
                    </svg>
                </div>
                <span className="user-name">{user?.username}</span>
                <svg
                    className={`dropdown-arrow ${isOpen ? 'open' : ''}`}
                    width="12"
                    height="12"
                    viewBox="0 0 12 12"
                    aria-hidden="true"
                >
                    <path
                        d="M3 4.5L6 7.5L9 4.5"
                        stroke="currentColor"
                        fill="none"
                        strokeWidth="2"
                    />
                </svg>
            </button>

            {isOpen && (
                <div className="user-dropdown-menu">
                    <div className="dropdown-header">
                        <div className="header-info">
                            <div className="header-signed-in">Signed in as</div>
                            <div className="header-username">
                                {user?.username}
                            </div>
                            <div className="header-player-id">
                                {`ID: ${user.playerId}`}
                            </div>
                        </div>
                    </div>
                    <div className="dropdown-divider"></div>

                    <button
                        className="dropdown-item"
                        onClick={() => handleItemClick('profile')}
                        type="button"
                    >
                        <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor" aria-hidden="true">
                            <path d="M8 0C3.6 0 0 3.6 0 8s3.6 8 8 8 8-3.6 8-8-3.6-8-8-8zm0 14c-3.3 0-6-2.7-6-6s2.7-6 6-6 6 2.7 6 6-2.7 6-6 6z" />
                            <path d="M8 4c-2.2 0-4 1.8-4 4s1.8 4 4 4 4-1.8 4-4-1.8-4-4-4zm0 6c-1.1 0-2-0.9-2-2s0.9-2 2-2 2 0.9 2 2-0.9 2-2 2z" />
                        </svg>
                        Profile
                    </button>

                    <button
                        className="dropdown-item"
                        onClick={() => handleItemClick('statistics')}
                        type="button"
                    >
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                            <path d="M7 24h-6v-6h6v6zm8-9h-6v9h6v-9zm8-4h-6v13h6v-13zm0-11l-6 1.221 1.716 1.708-6.85 6.733-3.001-3.002-7.841 7.797 1.41 1.418 6.427-6.39 2.991 2.993 8.28-8.137 1.667 1.66 1.201-6.001z" />
                        </svg>
                        Statistics
                    </button>

                    <button
                        className="dropdown-item"
                        onClick={() => handleItemClick('history')}
                        type="button"
                    >
                        <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor" aria-hidden="true">
                            <path d="M8 0C3.6 0 0 3.6 0 8s3.6 8 8 8 8-3.6 8-8-3.6-8-8-8zm0 14c-3.3 0-6-2.7-6-6s2.7-6 6-6 6 2.7 6 6-2.7 6-6 6z" />
                            <path d="M8 4v4l3 2" />
                        </svg>
                        Leaderboard
                    </button>

                    <button
                        className="dropdown-item"
                        onClick={() => handleItemClick('settings')}
                        type="button"
                    >
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
                            <path d="M24 13.616v-3.232c-1.651-.587-2.694-.752-3.219-2.019v-.001c-.527-1.271.1-2.134.847-3.707l-2.285-2.285c-1.561.742-2.433 1.375-3.707.847h-.001c-1.269-.526-1.435-1.576-2.019-3.219h-3.232c-.582 1.635-.749 2.692-2.019 3.219h-.001c-1.271.528-2.132-.098-3.707-.847l-2.285 2.285c.745 1.568 1.375 2.434.847 3.707-.527 1.271-1.584 1.438-3.219 2.02v3.232c1.632.58 2.692.749 3.219 2.019.53 1.282-.114 2.166-.847 3.707l2.285 2.286c1.562-.743 2.434-1.375 3.707-.847h.001c1.27.526 1.436 1.579 2.019 3.219h3.232c.582-1.636.75-2.69 2.027-3.222h.001c1.262-.524 2.12.101 3.698.851l2.285-2.286c-.744-1.563-1.375-2.433-.848-3.706.527-1.271 1.588-1.44 3.221-2.021zm-12 2.384c-2.209 0-4-1.791-4-4s1.791-4 4-4 4 1.791 4 4-1.791 4-4 4z" />
                        </svg>
                        Settings
                    </button>

                    {isAdmin && (
                        <>
                            <div className="dropdown-divider"></div>
                            <button
                                className="dropdown-item"
                                onClick={handleExportQuestions}
                            >
                                Export Questions
                            </button>

                            <button
                                className="dropdown-item"
                                onClick={() => fileInputRef.current.click()}
                            >
                                Import Questions
                            </button>

                            <input
                                ref={fileInputRef}
                                type="file"
                                accept=".json"
                                style={{ display: "none" }}
                                onChange={handleImportQuestions}
                            />
                        </>
                    )}

                    <div className="dropdown-divider" />
                    <button
                        className="dropdown-item logout-item"
                        onClick={handleLogout}
                        type="button"
                    >
                        <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor" aria-hidden="true">
                            <path d="M11 3h2c1.1 0 2 0.9 2 2v6c0 1.1-0.9 2-2 2h-2v2h2c2.2 0 4-1.8 4-4V5c0-2.2-1.8-4-4-4h-2v2z" />
                            <path d="M7.7 11.3L6.3 12.7 0.6 7 6.3 1.3 7.7 2.7 4.4 6 16 6 16 8 4.4 8z" />
                        </svg>
                        Sign Out
                    </button>
                </div>
            )}
        </div>
    );
};

export default UserDropdown;
