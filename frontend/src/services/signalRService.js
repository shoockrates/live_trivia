import * as signalR from '@microsoft/signalr';

class SignalRService {
  constructor() {
    this.connection = null;
    this.isConnected = false;
    this.listeners = new Map();
  }

  async startConnection(token) {
    if (this.connection && this.isConnected) {
      console.log('SignalR already connected');
      return;
    }

    try {
      this.connection = new signalR.HubConnectionBuilder()
        .withUrl('http://localhost:5216/gameHub', {
          accessTokenFactory: () => token,
          skipNegotiation: false,
          transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
        })
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: retryContext => {
            if (retryContext.elapsedMilliseconds < 60000) {
              return Math.random() * 10000;
            } else {
              return null;
            }
          }
        })
        .configureLogging(signalR.LogLevel.Information)
        .build();

      // Setup reconnection handlers
      this.connection.onreconnecting((error) => {
        console.log('SignalR reconnecting:', error);
        this.isConnected = false;
      });

      this.connection.onreconnected((connectionId) => {
        console.log('SignalR reconnected:', connectionId);
        this.isConnected = true;
      });

      this.connection.onclose((error) => {
        console.log('SignalR connection closed:', error);
        this.isConnected = false;
      });

      await this.connection.start();
      this.isConnected = true;
      console.log('SignalR Connected successfully');
    } catch (error) {
      console.error('Error starting SignalR connection:', error);
      this.isConnected = false;
      throw error;
    }
  }

  async stopConnection() {
    if (this.connection) {
      try {
        await this.connection.stop();
        this.isConnected = false;
        console.log('SignalR connection stopped');
      } catch (error) {
        console.error('Error stopping SignalR connection:', error);
      }
    }
  }

  async joinGameRoom(roomId) {
    if (!this.connection || !this.isConnected) {
      throw new Error('SignalR connection not established');
    }

    try {
      await this.connection.invoke('JoinGameRoom', roomId);
      console.log(`Joined game room: ${roomId}`);
    } catch (error) {
      console.error('Error joining game room:', error);
      throw error;
    }
  }

  async leaveGameRoom(roomId) {
    if (!this.connection || !this.isConnected) {
      console.log('Cannot leave room - not connected');
      return;
    }

    try {
      await this.connection.invoke('LeaveGameRoom', roomId);
      console.log(`Left game room: ${roomId}`);
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

  // Generic listener registration
  registerListener(eventName, callback) {
    if (!this.connection) {
      console.error('Cannot register listener - connection not initialized');
      return;
    }

    // Store the callback
    if (!this.listeners.has(eventName)) {
      this.listeners.set(eventName, []);
    }
    this.listeners.get(eventName).push(callback);

    // Register with SignalR
    this.connection.on(eventName, callback);
    console.log(`Registered listener for: ${eventName}`);
  }

  // Remove a specific listener
  removeListener(eventName, callback) {
    if (!this.connection) {
      return;
    }

    try {
      this.connection.off(eventName, callback);
      
      // Remove from our tracking
      if (this.listeners.has(eventName)) {
        const callbacks = this.listeners.get(eventName);
        const index = callbacks.indexOf(callback);
        if (index > -1) {
          callbacks.splice(index, 1);
        }
      }
      
      console.log(`Removed listener for: ${eventName}`);
    } catch (error) {
      console.error(`Error removing listener for ${eventName}:`, error);
    }
  }

  // Remove all listeners for an event
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
}

const signalRService = new SignalRService();
export default signalRService;
