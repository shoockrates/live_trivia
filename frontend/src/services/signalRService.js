import * as signalR from '@microsoft/signalr';

class SignalRService {
    constructor() {
        this.connection = null;
        this.isConnected = false;
        this.connectionPromise = null;
        this.listeners = new Map();
        this.reconnectAttempts = 0;
        this.maxReconnectAttempts = 10;
        this.currentRoomId = null; // Track current room
    }

    async startConnection(token) {
        // If already connected and healthy, return
        if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
            console.log('SignalR already connected');
            return;
        }

        // If connection is in progress, wait for it
        if (this.connectionPromise) {
            console.log('Connection already in progress, waiting...');
            return this.connectionPromise;
        }

        this.connectionPromise = new Promise(async (resolve, reject) => {
            try {
                // Stop existing connection if any
                if (this.connection) {
                    try {
                        await this.connection.stop();
                    } catch (e) {
                        console.log('Error stopping old connection:', e);
                    }
                }

                console.log('Creating new SignalR connection...');

                this.connection = new signalR.HubConnectionBuilder()
                    .withUrl('http://localhost:5216/gameHub', {
                        accessTokenFactory: () => token,
                        skipNegotiation: false,
                        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
                    })
                    .withAutomaticReconnect({
                        nextRetryDelayInMilliseconds: retryContext => {
                            // Exponential backoff with max delay
                            if (retryContext.elapsedMilliseconds < 10000) {
                                return Math.min(1000 * Math.pow(2, retryContext.previousRetryCount), 5000);
                            } else if (retryContext.elapsedMilliseconds < 60000) {
                                return 5000;
                            } else {
                                return 10000;
                            }
                        }
                    })
                    .configureLogging(signalR.LogLevel.Information)
                    .build();

                this.attachStoredListeners();

                // Setup reconnection handlers
                this.connection.onreconnecting((error) => {
                    console.log('SignalR reconnecting:', error);
                    this.isConnected = false;
                    this.reconnectAttempts++;
                });

                this.connection.onreconnected(async (connectionId) => {
                    console.log('SignalR reconnected:', connectionId);
                    this.isConnected = true;
                    this.reconnectAttempts = 0;

                    // Auto-rejoin current room if we have one
                    if (this.currentRoomId) {
                        console.log('Auto-rejoining room after reconnection:', this.currentRoomId);
                        try {
                            await this.connection.invoke('JoinGameRoom', this.currentRoomId);
                            console.log('Successfully rejoined room:', this.currentRoomId);
                        } catch (error) {
                            console.error('Failed to rejoin room after reconnection:', error);
                        }
                    }
                });

                this.connection.onclose((error) => {
                    console.log('SignalR connection closed:', error);
                    this.isConnected = false;
                    this.connectionPromise = null;

                    // Only attempt to reconnect if we haven't exceeded max attempts
                    if (this.reconnectAttempts < this.maxReconnectAttempts) {
                        console.log(`Will attempt to reconnect (attempt ${this.reconnectAttempts + 1}/${this.maxReconnectAttempts})`);
                        setTimeout(() => {
                            const token = localStorage.getItem('token');
                            if (token) {
                                this.startConnection(token).catch(e => {
                                    console.error('Reconnection failed:', e);
                                });
                            }
                        }, 2000);
                    }
                });

                // Start the connection
                console.log('Starting SignalR connection...');
                await this.connection.start();
                this.isConnected = true;
                this.reconnectAttempts = 0;
                console.log('SignalR Connected successfully, connection ID:', this.connection.connectionId);

                resolve();
            } catch (error) {
                console.error('Error starting SignalR connection:', error);
                this.isConnected = false;
                this.connectionPromise = null;
                reject(error);
            }
        });

        return this.connectionPromise;
    }

    attachStoredListeners() {
        if (!this.connection) return;

        for (const [eventName, callbacks] of this.listeners.entries()) {
            this.connection.off(eventName);
            for (const cb of callbacks) {
                this.connection.on(eventName, cb);
            }
        }
    }

    async stopConnection() {
        if (this.connection) {
            try {
                this.currentRoomId = null; // Clear current room
                await this.connection.stop();
                this.isConnected = false;
                this.connectionPromise = null;
                console.log('SignalR connection stopped');
            } catch (error) {
                console.error('Error stopping SignalR connection:', error);
            }
        }
    }

    async joinGameRoom(roomId) {
        // Wait for connection to be established
        if (!this.connection) {
            throw new Error('SignalR connection not initialized. Call startConnection first.');
        }

        // If not connected, wait for connection
        if (!this.isConnected && this.connectionPromise) {
            console.log('Waiting for connection to establish before joining room...');
            await this.connectionPromise;
        }

        // Check connection state
        if (this.connection.state !== signalR.HubConnectionState.Connected) {
            throw new Error(`Cannot join room. Connection state is: ${this.connection.state}`);
        }

        try {
            console.log(`Joining game room: ${roomId}`);
            await this.connection.invoke('JoinGameRoom', roomId);
            this.currentRoomId = roomId; // Track current room
            console.log(`Successfully joined game room: ${roomId}`);
        } catch (error) {
            console.error('Error joining game room:', error);
            throw error;
        }
    }

    async leaveGameRoom(roomId) {
        if (!this.connection || !this.isConnected) {
            console.log('Cannot leave room - not connected');
            this.currentRoomId = null;
            return;
        }

        try {
            await this.connection.invoke('LeaveGameRoom', roomId);
            console.log(`Left game room: ${roomId}`);
            if (this.currentRoomId === roomId) {
                this.currentRoomId = null;
            }
        } catch (error) {
            console.error('Error leaving game room:', error);
        }
    }

    // Event listener management
    onPlayerJoined(callback) {
        this.registerListener('PlayerJoined', callback);
    }

    onPlayerLeft(callback) {
        this.registerListener('PlayerLeft', callback);
    }

    onGameStarted(callback) {
        this.registerListener('GameStarted', callback);
    }

    onNextQuestion(callback) {
        this.registerListener('NextQuestion', callback);
    }

    onAnswerSubmitted(callback) {
        this.registerListener('AnswerSubmitted', callback);
    }

    onGameFinished(callback) {
        this.registerListener('GameFinished', callback);
    }

    onSettingsUpdated(callback) {
        this.registerListener('SettingsUpdated', callback);
    }

    onGameStartFailed(callback) {
        this.registerListener('GameStartFailed', callback);
    }

    onGameReset(callback) {
        this.registerListener('GameReset', callback);
    }

    // Voting-related listeners
    onCategoryVotingStarted(callback) {
        this.registerListener('CategoryVotingStarted', callback);
    }

    onCategoryVoteUpdated(callback) {
        this.registerListener('CategoryVoteUpdated', callback);
    }

    onCategoryVotingFinished(callback) {
        this.registerListener('CategoryVotingFinished', callback);
    }

    onCategoryRevoteStarted(callback) {
        this.registerListener('CategoryRevoteStarted', callback);
    }

    onCategoryVotingTimer(callback) {
        this.registerListener('CategoryVotingTimer', callback);
    }

    onGameStateSync(handler) {
        this.registerListener("GameStateSync", handler);
    }

    offGameStateSync(handler) {
        this.removeListener("GameStateSync", handler);
    }

    registerListener(eventName, callback) {
        if (!this.listeners.has(eventName)) {
            this.listeners.set(eventName, []);
        }

        const arr = this.listeners.get(eventName);
        if (!arr.includes(callback)) {
            arr.push(callback);
        }

        if (this.connection) {
            this.connection.on(eventName, callback);
            console.log(`Registered listener for: ${eventName}`);
        } else {
            console.log(`Stored listener for later (no connection yet): ${eventName}`);
        }
    }

    removeListener(eventName, callback) {
        if (!this.connection) {
            // Still clear stored listeners even if not connected
            if (!callback) {
                this.listeners.delete(eventName);
            } else if (this.listeners.has(eventName)) {
                const arr = this.listeners.get(eventName);
                const idx = arr.indexOf(callback);
                if (idx > -1) arr.splice(idx, 1);
                if (arr.length === 0) this.listeners.delete(eventName);
            }
            return;
        }

        try {
            if (!callback) {
                // remove ALL handlers for this event
                this.connection.off(eventName);
                this.listeners.delete(eventName);
                console.log(`Removed ALL listeners for: ${eventName}`);
                return;
            }

            this.connection.off(eventName, callback);

            if (this.listeners.has(eventName)) {
                const arr = this.listeners.get(eventName);
                const idx = arr.indexOf(callback);
                if (idx > -1) arr.splice(idx, 1);
                if (arr.length === 0) this.listeners.delete(eventName);
            }

            console.log(`Removed listener for: ${eventName}`);
        } catch (error) {
            console.error(`Error removing listener for ${eventName}:`, error);
        }
    }

    removeAllListeners(eventName) {
        if (!this.connection) {
            return;
        }

        try {
            if (eventName) {
                this.connection.off(eventName);
                this.listeners.delete(eventName);
            } else {
                // Remove all listeners
                this.listeners.forEach((_, event) => {
                    this.connection.off(event);
                });
                this.listeners.clear();
            }
            console.log(`Removed all listeners${eventName ? ` for: ${eventName}` : ''}`);
        } catch (error) {
            console.error('Error removing listeners:', error);
        }
    }

    // Hub method invocations
    async startGame(roomId) {
        if (!this.connection || !this.isConnected) {
            throw new Error('SignalR connection not established');
        }

        try {
            await this.connection.invoke('StartGame', roomId);
        } catch (error) {
            console.error('Error starting game:', error);
            throw error;
        }
    }


    async submitAnswer(roomId, questionId, selectedAnswers) {
        if (!this.connection || !this.isConnected) {
            throw new Error('SignalR connection not established');
        }

        try {
            await this.connection.invoke('SubmitAnswer', roomId, questionId, selectedAnswers);
        } catch (error) {
            console.error('Error submitting answer:', error);
            throw error;
        }
    }


    async nextQuestion(roomId) {
        if (!this.connection || !this.isConnected) {
            throw new Error('SignalR connection not established');
        }

        try {
            await this.connection.invoke('NextQuestion', roomId);
        } catch (error) {
            console.error('Error advancing to next question:', error);
            throw error;
        }
    }

    async updateGameSettings(roomId, settings) {
        if (!this.connection || !this.isConnected) {
            throw new Error('SignalR connection not established');
        }

        try {
            await this.connection.invoke('UpdateGameSettings', roomId, settings);
        } catch (error) {
            console.error('Error updating game settings:', error);
            throw error;
        }
    }

    getConnectionState() {
        if (!this.connection) {
            return 'Disconnected';
        }
        return signalR.HubConnectionState[this.connection.state];
    }

    async waitForConnection(maxWaitMs = 10000) {
        const startTime = Date.now();

        while (!this.isConnected && (Date.now() - startTime) < maxWaitMs) {
            if (this.connectionPromise) {
                try {
                    await this.connectionPromise;
                    return;
                } catch (e) {
                    console.error('Connection failed while waiting:', e);
                    throw e;
                }
            }
            await new Promise(resolve => setTimeout(resolve, 100));
        }

        if (!this.isConnected) {
            throw new Error('Connection timeout');
        }
    }

    async startCategoryVoting(roomId, categories) {
        if (!this.connection || !this.isConnected) {
            throw new Error('SignalR connection not established');
        }

        try {
            await this.connection.invoke('StartCategoryVoting', roomId, categories);
            console.log('StartCategoryVoting invoked', { roomId, categories });
        } catch (error) {
            console.error('Error starting category voting:', error);
            throw error;
        }
    }

    async submitCategoryVote(roomId, category) {
        if (!this.connection || !this.isConnected) {
            throw new Error('SignalR connection not established');
        }

        try {
            await this.connection.invoke('SubmitCategoryVote', roomId, category);
            console.log('SubmitCategoryVote invoked', { roomId, category });
        } catch (error) {
            console.error('Error submitting category vote:', error);
            throw error;
        }
    }

    async endCategoryVoting(roomId) {
        if (!this.connection || !this.isConnected) {
            throw new Error('SignalR connection not established');
        }

        try {
            await this.connection.invoke('EndCategoryVoting', roomId);
            console.log('EndCategoryVoting invoked', { roomId });
        } catch (error) {
            console.error('Error ending category voting:', error);
            throw error;
        }
    }
}

const signalRService = new SignalRService();
export default signalRService;
