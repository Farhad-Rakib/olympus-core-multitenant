# Notification System - Backend to Frontend Integration Guide

## Overview

The notification system provides real-time communication between the backend and frontend using **SignalR**. Notifications are stored in the database and pushed to connected clients in real-time.

---

## Architecture

### Components

1. **Backend**
   - `NotificationService` - Business logic for creating/managing notifications
   - `NotificationHub` - SignalR hub for real-time communication
   - `Notification` Entity - Database model
   - `NotificationRepository` - Data access layer
   - `NotificationConfiguration` - EF Core configuration

2. **Frontend**
   - SignalR client connection
   - Event listeners for notification events
   - UI components to display notifications

3. **Database**
   - `notifications` table - Stores all notifications
   - Columns: Id, UserId, Type, Message, IsRead, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy

---

## Backend Implementation

### 1. Notification Entity

```csharp
// Location: OlympusCoreMultitenant.Domain/Entities/Notification.cs
public sealed class Notification : BaseEntity
{
    public long UserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public User? User { get; set; }
}
```

**Fields:**
- `Id` - Primary key (auto-generated)
- `UserId` - Target user for the notification
- `Type` - Classification (e.g., "info", "warning", "error", "success")
- `Message` - Notification content
- `IsRead` - Read status
- `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy` - Audit fields

### 2. Notification Service

```csharp
// Location: OlympusCoreMultitenant.Application/Notifications/INotificationService.cs
public interface INotificationService
{
    Task<IReadOnlyList<NotificationDto>> GetNotificationsAsync(long userId, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(long notificationId, CancellationToken cancellationToken = default);
    Task<NotificationDto> CreateNotificationAsync(long userId, string type, string message, CancellationToken cancellationToken = default);
    Task DeleteNotificationAsync(long notificationId, CancellationToken cancellationToken = default);
}
```

### 3. Creating Notifications from Backend

**Example 1: Create a notification after user registration**

```csharp
// In AuthService.cs
public async Task<UserRegistrationResult> RegisterAsync(RegisterUserRequestDto request, CancellationToken cancellationToken = default)
{
    // ... registration logic ...
    
    // Create welcome notification
    var notification = new Notification(
        userId: user.Id,
        type: "info",
        message: "Welcome to our application!"
    );
    
    await _notificationRepository.AddAsync(notification, cancellationToken);
    await _unitOfWork.SaveChangesAsync(cancellationToken);
    
    // Push to frontend via SignalR
    await _notificationHub.Clients.User(user.Id.ToString()).SendAsync("ReceiveNotification", notification);
    
    return new UserRegistrationResult(/*...*/);
}
```

**Example 2: Create a notification for an event**

```csharp
// In some service/controller
public async Task UpdateProfileAsync(long userId, UpdateProfileRequestDto request, CancellationToken cancellationToken = default)
{
    // ... update logic ...
    
    // Create notification
    var notification = new Notification(
        userId: userId,
        type: "success",
        message: "Your profile has been updated successfully."
    );
    
    await _notificationRepository.AddAsync(notification, cancellationToken);
    await _unitOfWork.SaveChangesAsync(cancellationToken);
}
```

### 4. NotificationHub (SignalR)

```csharp
// Location: OlympusCoreMultitenant.Api/Hubs/NotificationHub.cs
using Microsoft.AspNetCore.SignalR;

namespace OlympusCoreMultitenant.Api.Hubs;

public class NotificationHub : Hub
{
    /// <summary>
    /// Called when a client connects to the hub
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst("sub")?.Value ?? Context.ConnectionId;
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Send notification to specific user
    /// </summary>
    public async Task SendNotificationToUser(long userId, string type, string message)
    {
        await Clients.Group($"user_{userId}").SendAsync("ReceiveNotification", new
        {
            Id = Guid.NewGuid(),
            Type = type,
            Message = message,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Send notification to all connected clients
    /// </summary>
    public async Task BroadcastNotification(string type, string message)
    {
        await Clients.All.SendAsync("ReceiveNotification", new
        {
            Id = Guid.NewGuid(),
            Type = type,
            Message = message,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Mark notification as read
    /// </summary>
    public async Task MarkNotificationAsRead(long notificationId)
    {
        // Implement logic to mark as read
        await Clients.All.SendAsync("NotificationMarkedAsRead", notificationId);
    }
}
```

---

## Frontend Implementation

### 1. Initialize SignalR Connection

**React Example:**

```typescript
// hooks/useNotifications.ts
import { useEffect, useState, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';

interface Notification {
  id: number;
  type: 'info' | 'warning' | 'error' | 'success';
  message: string;
  isRead: boolean;
  createdAt: string;
}

export const useNotifications = () => {
  const [notifications, setNotifications] = useState<Notification[]>([]);
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);

  useEffect(() => {
    // Create SignalR connection
    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl('https://localhost:7000/hubs/notifications', {
        accessTokenFactory: () => localStorage.getItem('token') || ''
      })
      .withAutomaticReconnect()
      .withHubProtocol(new signalR.JsonHubProtocol())
      .build();

    // Define handlers
    newConnection.on('ReceiveNotification', (notification: Notification) => {
      setNotifications(prev => [notification, ...prev]);
      
      // Optional: Show toast notification
      showToast(notification.message, notification.type);
    });

    newConnection.on('NotificationMarkedAsRead', (notificationId: number) => {
      setNotifications(prev =>
        prev.map(n => n.id === notificationId ? { ...n, isRead: true } : n)
      );
    });

    // Start connection
    newConnection.start()
      .then(() => console.log('SignalR connected'))
      .catch(err => console.error('SignalR connection failed:', err));

    setConnection(newConnection);

    // Cleanup
    return () => {
      newConnection.stop();
    };
  }, []);

  return { notifications, connection };
};
```

### 2. Display Notifications Component

```typescript
// components/NotificationCenter.tsx
import React from 'react';
import { useNotifications } from '../hooks/useNotifications';

export const NotificationCenter: React.FC = () => {
  const { notifications } = useNotifications();

  const unreadCount = notifications.filter(n => !n.isRead).length;

  const getNotificationIcon = (type: string) => {
    switch (type) {
      case 'success':
        return '✓';
      case 'error':
        return '✕';
      case 'warning':
        return '⚠';
      case 'info':
      default:
        return 'ℹ';
    }
  };

  const getNotificationColor = (type: string) => {
    switch (type) {
      case 'success':
        return 'green';
      case 'error':
        return 'red';
      case 'warning':
        return 'orange';
      case 'info':
      default:
        return 'blue';
    }
  };

  return (
    <div className="notification-center">
      <div className="notification-bell">
        🔔 {unreadCount > 0 && <span className="badge">{unreadCount}</span>}
      </div>

      <div className="notification-list">
        {notifications.length === 0 ? (
          <p>No notifications</p>
        ) : (
          notifications.map(notification => (
            <div
              key={notification.id}
              className={`notification-item ${notification.type} ${
                !notification.isRead ? 'unread' : ''
              }`}
              style={{ borderLeft: `4px solid ${getNotificationColor(notification.type)}` }}
            >
              <span className="icon">{getNotificationIcon(notification.type)}</span>
              <div className="content">
                <p className="message">{notification.message}</p>
                <small className="time">{new Date(notification.createdAt).toLocaleString()}</small>
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  );
};
```

### 3. CSS Styling

```css
/* styles/notifications.css */
.notification-center {
  position: relative;
  width: 300px;
  max-height: 500px;
  border: 1px solid #ddd;
  border-radius: 8px;
  background: white;
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
}

.notification-bell {
  position: relative;
  padding: 12px;
  font-size: 18px;
  cursor: pointer;
}

.notification-bell .badge {
  position: absolute;
  top: 8px;
  right: 8px;
  background: red;
  color: white;
  border-radius: 50%;
  width: 20px;
  height: 20px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 12px;
}

.notification-list {
  max-height: 400px;
  overflow-y: auto;
}

.notification-item {
  padding: 12px;
  border-bottom: 1px solid #eee;
  display: flex;
  gap: 10px;
  align-items: flex-start;
  transition: background-color 0.2s;
}

.notification-item:hover {
  background-color: #f9f9f9;
}

.notification-item.unread {
  background-color: #f0f7ff;
  font-weight: 500;
}

.notification-item .icon {
  font-size: 18px;
  min-width: 20px;
}

.notification-item .content {
  flex: 1;
}

.notification-item .message {
  margin: 0;
  font-size: 14px;
}

.notification-item .time {
  display: block;
  margin-top: 4px;
  color: #999;
  font-size: 12px;
}
```

---

## API Endpoints

### Get User Notifications

**Endpoint:** `GET /api/v1/notifications`

```typescript
// Frontend
const fetchNotifications = async (userId: long) => {
  const response = await fetch(`/api/v1/notifications?userId=${userId}`, {
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });
  return response.json();
};
```

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "id": 1,
      "type": "info",
      "message": "Welcome to our application!",
      "isRead": false,
      "createdAt": "2026-05-14T12:34:56Z"
    }
  ]
}
```

### Mark as Read

**Endpoint:** `POST /api/v1/notifications/{id}/mark-as-read`

```typescript
// Frontend
const markAsRead = async (notificationId: number) => {
  const response = await fetch(`/api/v1/notifications/${notificationId}/mark-as-read`, {
    method: 'POST',
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });
  return response.json();
};
```

### Delete Notification

**Endpoint:** `DELETE /api/v1/notifications/{id}`

```typescript
// Frontend
const deleteNotification = async (notificationId: number) => {
  const response = await fetch(`/api/v1/notifications/${notificationId}`, {
    method: 'DELETE',
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });
  return response.json();
};
```

---

## Notification Types

Use standardized notification types for consistent UI rendering:

| Type | Usage | Color |
|------|-------|-------|
| `info` | General information | Blue |
| `success` | Operation succeeded | Green |
| `warning` | Warning message | Orange |
| `error` | Error occurred | Red |

---

## Usage Examples

### Example 1: User Registration Flow

**Backend:**
```csharp
// AuthController.cs
[HttpPost("register")]
public async Task<IActionResult> Register([FromBody] RegisterUserRequestDto request)
{
    var result = await _authService.RegisterAsync(request);
    
    // Send welcome notification via SignalR
    await _notificationHub.Clients.User(result.User.Id.ToString())
        .SendAsync("ReceiveNotification", new
        {
            type = "success",
            message = $"Welcome {result.User.FullName}! Your account has been created."
        });

    return CreatedAtAction(nameof(Register), new { id = result.User.Id }, result);
}
```

**Frontend:**
```typescript
// Components/RegisterForm.tsx
const handleRegister = async (formData) => {
  try {
    const response = await api.post('/auth/register', formData);
    // SignalR will automatically receive and display the notification
    console.log('Registration successful');
  } catch (error) {
    // Error notification will be sent
  }
};
```

### Example 2: Real-time Admin Broadcast

**Backend:**
```csharp
// AdminController.cs
[HttpPost("broadcast-notification")]
[Authorize(Policy = Permissions.AdminAccess)]
public async Task<IActionResult> BroadcastNotification([FromBody] BroadcastNotificationRequest request)
{
    await _notificationHub.Clients.All.SendAsync("ReceiveNotification", new
    {
        type = "info",
        message = request.Message
    });

    return Ok(ApiResponse.SuccessResponse("Notification broadcast successfully"));
}
```

**Frontend:**
```typescript
// All connected clients will receive this notification automatically
```

### Example 3: Handling Notifications in Layout

```typescript
// Layout.tsx
import { NotificationCenter } from './components/NotificationCenter';

export const Layout: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  return (
    <div className="app-layout">
      <header className="navbar">
        <h1>My App</h1>
        <NotificationCenter />
      </header>
      <main>{children}</main>
    </div>
  );
};
```

---

## Connection Setup in Program.cs

Ensure SignalR is configured:

```csharp
// Program.cs
builder.Services.AddSignalR();

var app = builder.Build();

// Add mapping for notification hub
app.MapHub<NotificationHub>("/hubs/notifications");
```

---

## Common Patterns

### 1. Notification on Async Operation

```csharp
// Backend
public async Task UpdateUserProfileAsync(long userId, UpdateProfileRequest request)
{
    try
    {
        await _userService.UpdateProfileAsync(userId, request);
        
        var notification = new Notification(
            userId: userId,
            type: "success",
            message: "Profile updated successfully"
        );
        
        await _notificationRepository.AddAsync(notification);
        await _unitOfWork.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        var notification = new Notification(
            userId: userId,
            type: "error",
            message: $"Profile update failed: {ex.Message}"
        );
        
        await _notificationRepository.AddAsync(notification);
        await _unitOfWork.SaveChangesAsync();
    }
}
```

### 2. Bulk Notifications

```csharp
// Send notifications to multiple users
public async Task NotifyUsersAsync(List<long> userIds, string type, string message)
{
    var notifications = userIds.Select(userId => 
        new Notification(userId, type, message)
    ).ToList();
    
    await _notificationRepository.AddRangeAsync(notifications);
    await _unitOfWork.SaveChangesAsync();
    
    // Push to each user
    foreach (var userId in userIds)
    {
        await _notificationHub.Clients.Group($"user_{userId}")
            .SendAsync("ReceiveNotification", new { type, message });
    }
}
```

### 3. Scheduled Notifications

```csharp
// Using background jobs (e.g., Hangfire)
public void ScheduleNotification(long userId, string message, DateTime scheduleTime)
{
    BackgroundJob.Schedule(() => 
        _notificationService.CreateNotificationAsync(
            userId, 
            "info", 
            message
        ), 
        scheduleTime
    );
}
```

---

## Troubleshooting

### Issue: Notifications not appearing
- **Check:** SignalR connection established (`connection.start()` successful)
- **Check:** User is authenticated and JWT token is valid
- **Check:** Hub is mapped correctly in Program.cs
- **Check:** Browser console for connection errors

### Issue: Connection keeps disconnecting
- **Solution:** Enable automatic reconnect with backoff strategy
```typescript
.withAutomaticReconnect([0, 0, 5000])
```

### Issue: Notifications slow to appear
- **Optimize:** Use database indices on UserId and IsRead columns (already configured)
- **Optimize:** Implement pagination for notification history
- **Optimize:** Batch notifications when possible

---

## Best Practices

1. **Always validate user access** - Ensure users only receive notifications meant for them
2. **Use appropriate types** - Use standardized notification types for UI consistency
3. **Limit notification count** - Archive old notifications or implement retention policy
4. **Handle offline users** - Notifications stored in DB will be retrieved on next login
5. **Rate limiting** - Avoid notification spam, implement cooldowns if necessary
6. **Audit trail** - Keep CreatedBy field to track notification origin
7. **Testing** - Test SignalR connections with unit and integration tests

---

## Files Reference

- **Backend Models:** `/OlympusCoreMultitenant.Domain/Entities/Notification.cs`
- **Service Interface:** `/OlympusCoreMultitenant.Application/Notifications/INotificationService.cs`
- **SignalR Hub:** `/OlympusCoreMultitenant.Api/Hubs/NotificationHub.cs`
- **Entity Configuration:** `/OlympusCoreMultitenant.Persistence/Configurations/NotificationConfiguration.cs`
- **Migration:** `/OlympusCoreMultitenant.Persistence/Migrations/20260514123906_AddNotificationTable.cs`

