# Track 3: Web UI Fixes for Junior Developer

## Overview
The Web UI project has compilation errors because it expects certain properties and methods that don't exist in the Core models. This document provides step-by-step instructions to fix these issues.

---

## 🔧 Fix #1: OrchestrationProgress Properties

### Problem
The file `Services/OrchestrationService.cs` expects properties on `OrchestrationProgress` that don't exist.

### Location
`src/OrchestratorChat.Web/Services/OrchestrationService.cs` (lines 61-66)

### Current Code (BROKEN)
```csharp
CurrentStep = progress?.CurrentStep ?? 0,  // Line 61 - ERROR
CompletedSteps = progress?.CompletedSteps ?? new List<string>(),  // Line 62
Progress = progress?.ProgressPercentage ?? 0,
Status = progress?.Status ?? "Unknown",  // Line 64
ErrorMessage = progress?.ErrorMessage,  // Line 65
Data = progress?.Data  // Line 66
```

### Solution
**Option A: Use existing properties**

Replace lines 61-66 with:
```csharp
CurrentStep = progress?.CurrentStepIndex ?? 0,
CompletedSteps = new List<string>(),  // Create empty list for now
Progress = progress?.ProgressPercentage ?? 0,
Status = "Running",  // Use a fixed status
ErrorMessage = string.Empty,  // No error message for now
Data = new Dictionary<string, object>()  // Empty data dictionary
```

**Option B: Create a Web-specific model**

Create a new file: `src/OrchestratorChat.Web/Models/WebOrchestrationProgress.cs`
```csharp
namespace OrchestratorChat.Web.Models;

public class WebOrchestrationProgress
{
    public int CurrentStep { get; set; }
    public List<string> CompletedSteps { get; set; } = new();
    public double Progress { get; set; }
    public string Status { get; set; } = "Unknown";
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object>? Data { get; set; }
    
    // Constructor to convert from Core model
    public WebOrchestrationProgress(Core.Orchestration.OrchestrationProgress? coreProgress)
    {
        if (coreProgress != null)
        {
            CurrentStep = coreProgress.CurrentStepIndex;
            Progress = coreProgress.ProgressPercentage;
            // Set other properties with defaults
            CompletedSteps = new List<string>();
            Status = coreProgress.CurrentStepIndex > 0 ? "Running" : "Starting";
        }
    }
}
```

Then update `OrchestrationService.cs` to use `WebOrchestrationProgress` instead.

---

## 🔧 Fix #2: Attachment Properties

### Problem
The file `Components/AttachmentChip.razor` expects `Type` and `Name` properties on `Attachment` that don't exist.

### Location
`src/OrchestratorChat.Web/Components/AttachmentChip.razor` (lines 16, 28)

### Current Code (BROKEN)
```razor
@switch (Attachment.Type)  // Line 16 - ERROR
{
    case "image":
        icon = Icons.Material.Filled.Image;
        break;
    // ...
}

<MudText Typo="Typo.body2">@Attachment.Name</MudText>  // Line 28 - ERROR
```

### Solution

**Step 1:** Check what properties `Attachment` actually has.
Look in `src/OrchestratorChat.Core/Models/Attachment.cs` or search for the class definition.

**Step 2:** If the properties exist with different names (like `FileType` and `FileName`), update the component:

```razor
@* Line 16 - Use actual property name *@
@switch (Attachment.MimeType)  // or whatever the actual property is
{
    case "image/png":
    case "image/jpeg":
        icon = Icons.Material.Filled.Image;
        break;
    case "application/pdf":
        icon = Icons.Material.Filled.PictureAsPdf;
        break;
    default:
        icon = Icons.Material.Filled.AttachFile;
        break;
}

@* Line 28 - Use actual property name *@
<MudText Typo="Typo.body2">@Attachment.FileName</MudText>  // or whatever it actually is
```

**Step 3:** If the properties don't exist at all, you have two options:

**Option A:** Use the FileName to determine the type:
```razor
@code {
    private string GetFileType()
    {
        if (string.IsNullOrEmpty(Attachment.FileName))
            return "file";
            
        var extension = Path.GetExtension(Attachment.FileName).ToLower();
        return extension switch
        {
            ".jpg" or ".jpeg" or ".png" or ".gif" => "image",
            ".pdf" => "pdf",
            ".doc" or ".docx" => "document",
            ".txt" => "text",
            _ => "file"
        };
    }
    
    private string GetFileName()
    {
        return Attachment.FileName ?? "Unnamed File";
    }
}
```

Then use these methods in the Razor markup:
```razor
@switch (GetFileType())
{
    // ... cases
}

<MudText Typo="Typo.body2">@GetFileName()</MudText>
```

---

## 🔧 Fix #3: Session.ParticipantAgents

### Problem
The file `Components/SessionIndicator.razor` expects `ParticipantAgents` property on `Session`.

### Location
`src/OrchestratorChat.Web/Components/SessionIndicator.razor` (line 38)

### Current Code (BROKEN)
```razor
<MudText Typo="Typo.caption">
    @_currentSession.ParticipantAgents?.Count agents active
</MudText>
```

### Solution

**Option A:** Use a different property if it exists:
```razor
<MudText Typo="Typo.caption">
    @(_currentSession.Messages?.Count ?? 0) messages
</MudText>
```

**Option B:** Create a method to get agent count:
```csharp
@code {
    private int GetAgentCount()
    {
        // If there's an AgentIds property:
        // return _currentSession?.AgentIds?.Count ?? 0;
        
        // Or just return a placeholder:
        return 0;
    }
}
```

Then use it:
```razor
<MudText Typo="Typo.caption">
    @GetAgentCount() agents active
</MudText>
```

---

## 🔧 Fix #4: IAgent.DisposeAsync

### Problem
The file `Services/AgentService.cs` tries to call `DisposeAsync()` on `IAgent`.

### Location
`src/OrchestratorChat.Web/Services/AgentService.cs` (line 92)

### Current Code (BROKEN)
```csharp
await agent.DisposeAsync();  // ERROR - DisposeAsync doesn't exist
```

### Solution

**Step 1:** Check if `IAgent` implements `IDisposable`:

Look in `src/OrchestratorChat.Core/Agents/IAgent.cs`

**Step 2:** Fix based on what you find:

**If IAgent has Dispose():**
```csharp
// Replace line 92 with:
if (agent is IDisposable disposable)
{
    disposable.Dispose();
}
```

**If IAgent has Shutdown() or similar:**
```csharp
// Replace line 92 with:
await agent.ShutdownAsync();  // or whatever method exists
```

**If no cleanup method exists:**
```csharp
// Just remove the line or comment it out:
// await agent.DisposeAsync();  // TODO: Add cleanup when available
```

---

## 🔧 Fix #5: EventCallback Ambiguity

### Problem
Pages/ChatInterface.razor has an ambiguous EventCallback call.

### Location
Generated file, but the source is in `Pages/ChatInterface.razor`

### Solution

Find the line with `OnAttach` callback (around line 197-198) and make it explicit:

**Current (might be):**
```razor
<MessageInput OnSendMessage="@SendMessage" 
             IsEnabled="@(!_isProcessing)"
             OnAttach="@AttachFile" />
```

**Fix - Make the callback explicit:**
```razor
<MessageInput OnSendMessage="@SendMessage" 
             IsEnabled="@(!_isProcessing)"
             OnAttach="@((string s) => AttachFile())" />
```

Or if AttachFile needs no parameters:
```razor
<MessageInput OnSendMessage="@SendMessage" 
             IsEnabled="@(!_isProcessing)"
             OnAttach="@(() => AttachFile())" />
```

---

## 🔧 Fix #6: Missing SessionService Methods

### Problem
`SessionService` needs to implement methods that use the Core models correctly.

### Location
`src/OrchestratorChat.Web/Services/SessionService.cs`

### Solution

Update the SessionService to handle the actual Core model structure:

```csharp
public class SessionService : ISessionService
{
    private readonly ISessionManager _sessionManager;
    
    public SessionService(ISessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }
    
    public async Task<Session?> GetCurrentSessionAsync()
    {
        try
        {
            return await _sessionManager.GetCurrentSessionAsync();
        }
        catch
        {
            return null;
        }
    }
    
    public async Task<List<SessionSummary>> GetRecentSessionsAsync(int count = 20)
    {
        try
        {
            return await _sessionManager.GetRecentSessions(count);
        }
        catch
        {
            return new List<SessionSummary>();
        }
    }
}
```

---

## 📋 Testing Your Fixes

After making each fix:

1. **Save the file**
2. **Try to build just the Web project:**
   ```bash
   cd src/OrchestratorChat.Web
   dotnet build
   ```
3. **Check for fewer errors**
4. **Move to the next fix**

---

## 🎯 Quick Fix Summary

If you want to get it building quickly without perfect functionality:

1. **OrchestrationService.cs**: Use empty/default values for missing properties
2. **AttachmentChip.razor**: Use `FileName` only, ignore type
3. **SessionIndicator.razor**: Show message count instead of agent count
4. **AgentService.cs**: Remove or comment out the DisposeAsync line
5. **ChatInterface.razor**: Make callbacks explicit with `() =>`

---

## ⚠️ Important Notes

- **Don't modify Core models** - Only fix the Web project files
- **Use defensive coding** - Add null checks and try-catch blocks
- **Comment your workarounds** - Add TODO comments for temporary fixes
- **Test incrementally** - Build after each fix to see progress

---

## 🆘 If You Get Stuck

1. **Check the actual Core model** - Use "Go to Definition" (F12) to see what properties actually exist
2. **Use placeholders** - It's okay to return empty lists or zero counts temporarily
3. **Ask for help** - If a fix seems too complex, document what you tried and ask for guidance

Good luck! Start with Fix #1 and work your way down. The goal is to get the project compiling, even if some features don't work perfectly yet.