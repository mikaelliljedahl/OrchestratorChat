# API Contracts Documentation

## Overview
This document provides comprehensive API contracts for OrchestratorChat, including REST endpoints, SignalR hub methods, and data contracts.

## REST API Endpoints

### Base Configuration
```
Base URL: https://localhost:5001/api
Authentication: JWT Bearer Token
Content-Type: application/json
```

### Session Management APIs

#### Create Session
```http
POST /api/sessions
Authorization: Bearer {token}
```

**Request Body:**
```json
{
  "name": "Development Session",
  "type": "MultiAgent",
  "agentIds": ["agent-1", "agent-2"],
  "workingDirectory": "C:\\Projects\\MyApp",
  "projectId": "project-123",
  "configuration": {
    "persistHistory": true,
    "maxMessages": 1000,
    "initialContext": {
      "task": "Code review and refactoring"
    }
  }
}
```

**Response (200 OK):**
```json
{
  "id": "session-abc123",
  "name": "Development Session",
  "type": "MultiAgent",
  "status": "Active",
  "createdAt": "2024-01-30T10:00:00Z",
  "participantAgents": [
    {
      "id": "agent-1",
      "name": "Claude Assistant",
      "type": "Claude",
      "status": "Ready"
    },
    {
      "id": "agent-2",
      "name": "Saturn Developer",
      "type": "Saturn",
      "status": "Ready"
    }
  ],
  "workingDirectory": "C:\\Projects\\MyApp",
  "projectId": "project-123"
}
```

#### Get Session
```http
GET /api/sessions/{sessionId}
Authorization: Bearer {token}
```

**Response (200 OK):**
```json
{
  "id": "session-abc123",
  "name": "Development Session",
  "type": "MultiAgent",
  "status": "Active",
  "createdAt": "2024-01-30T10:00:00Z",
  "lastActivityAt": "2024-01-30T11:30:00Z",
  "messages": [
    {
      "id": "msg-1",
      "content": "Hello, how can I help?",
      "role": "Assistant",
      "agentId": "agent-1",
      "timestamp": "2024-01-30T10:01:00Z"
    }
  ],
  "participantAgents": ["agent-1", "agent-2"],
  "statistics": {
    "messageCount": 42,
    "totalTokensUsed": 15000,
    "duration": "01:30:00"
  }
}
```

#### List Sessions
```http
GET /api/sessions?status={status}&projectId={projectId}&limit={limit}
Authorization: Bearer {token}
```

**Query Parameters:**
- `status` (optional): Filter by status (Active, Paused, Completed)
- `projectId` (optional): Filter by project
- `limit` (optional): Maximum number of results (default: 20)
- `offset` (optional): Pagination offset

**Response (200 OK):**
```json
{
  "sessions": [
    {
      "id": "session-abc123",
      "name": "Development Session",
      "type": "MultiAgent",
      "status": "Active",
      "createdAt": "2024-01-30T10:00:00Z",
      "lastActivityAt": "2024-01-30T11:30:00Z",
      "messageCount": 42,
      "agentCount": 2
    }
  ],
  "totalCount": 150,
  "hasMore": true
}
```

#### Update Session
```http
PUT /api/sessions/{sessionId}
Authorization: Bearer {token}
```

**Request Body:**
```json
{
  "name": "Updated Session Name",
  "status": "Paused",
  "context": {
    "additionalInfo": "New context data"
  }
}
```

#### Delete Session
```http
DELETE /api/sessions/{sessionId}
Authorization: Bearer {token}
```

**Response (204 No Content)**

### Agent Management APIs

#### List Agents
```http
GET /api/agents?type={type}&status={status}
Authorization: Bearer {token}
```

**Response (200 OK):**
```json
{
  "agents": [
    {
      "id": "agent-1",
      "name": "Claude Assistant",
      "type": "Claude",
      "status": "Ready",
      "description": "General purpose AI assistant",
      "capabilities": {
        "supportsStreaming": true,
        "supportsTools": true,
        "supportsFileOperations": true,
        "availableTools": ["read", "write", "bash", "grep"],
        "maxTokens": 100000
      },
      "configuration": {
        "model": "claude-3-sonnet",
        "temperature": 0.7,
        "maxTokens": 4096
      },
      "statistics": {
        "totalSessions": 150,
        "totalMessages": 3500,
        "totalTokensUsed": 1500000
      }
    }
  ]
}
```

#### Create Agent
```http
POST /api/agents
Authorization: Bearer {token}
```

**Request Body:**
```json
{
  "name": "Custom Agent",
  "type": "Saturn",
  "description": "Specialized development agent",
  "workingDirectory": "C:\\Projects",
  "configuration": {
    "model": "claude-3-opus",
    "temperature": 0.3,
    "maxTokens": 8192,
    "systemPrompt": "You are an expert developer",
    "enabledTools": ["read", "write", "bash"],
    "requireApproval": true,
    "customSettings": {
      "provider": "OpenRouter",
      "apiKey": "sk-..."
    }
  }
}
```

#### Get Agent
```http
GET /api/agents/{agentId}
Authorization: Bearer {token}
```

#### Update Agent Configuration
```http
PUT /api/agents/{agentId}/configuration
Authorization: Bearer {token}
```

**Request Body:**
```json
{
  "model": "claude-3-opus",
  "temperature": 0.5,
  "maxTokens": 8192,
  "systemPrompt": "Updated system prompt",
  "enabledTools": ["read", "write"],
  "requireApproval": false
}
```

#### Delete Agent
```http
DELETE /api/agents/{agentId}
Authorization: Bearer {token}
```

#### Get Agent Health
```http
GET /api/agents/{agentId}/health
Authorization: Bearer {token}
```

**Response (200 OK):**
```json
{
  "agentId": "agent-1",
  "status": "Healthy",
  "lastCheckTime": "2024-01-30T12:00:00Z",
  "responseTime": 150,
  "metrics": {
    "memoryUsage": 256000000,
    "cpuUsage": 15.5,
    "activeConnections": 3
  }
}
```

### Message APIs

#### Send Message
```http
POST /api/sessions/{sessionId}/messages
Authorization: Bearer {token}
```

**Request Body:**
```json
{
  "content": "Please review this code",
  "agentId": "agent-1",
  "attachments": [
    {
      "fileName": "code.js",
      "mimeType": "text/javascript",
      "content": "base64encodedcontent"
    }
  ],
  "metadata": {
    "priority": "high"
  }
}
```

**Response (202 Accepted):**
```json
{
  "messageId": "msg-xyz789",
  "status": "Processing",
  "estimatedResponseTime": 5000
}
```

#### Get Messages
```http
GET /api/sessions/{sessionId}/messages?limit={limit}&before={messageId}
Authorization: Bearer {token}
```

**Response (200 OK):**
```json
{
  "messages": [
    {
      "id": "msg-1",
      "content": "Hello",
      "role": "User",
      "timestamp": "2024-01-30T10:00:00Z",
      "agentId": null,
      "attachments": []
    },
    {
      "id": "msg-2",
      "content": "Hello! How can I help?",
      "role": "Assistant",
      "timestamp": "2024-01-30T10:00:05Z",
      "agentId": "agent-1",
      "tokenUsage": {
        "inputTokens": 10,
        "outputTokens": 15,
        "totalTokens": 25
      }
    }
  ],
  "hasMore": true
}
```

### Orchestration APIs

#### Create Orchestration Plan
```http
POST /api/orchestration/plans
Authorization: Bearer {token}
```

**Request Body:**
```json
{
  "sessionId": "session-abc123",
  "goal": "Refactor the authentication module and add tests",
  "strategy": "Adaptive",
  "availableAgentIds": ["agent-1", "agent-2", "agent-3"],
  "constraints": {
    "maxSteps": 10,
    "timeout": 1800000,
    "requireApproval": true
  }
}
```

**Response (200 OK):**
```json
{
  "planId": "plan-123",
  "sessionId": "session-abc123",
  "steps": [
    {
      "order": 1,
      "agentId": "agent-1",
      "task": "Analyze current authentication module",
      "dependsOn": [],
      "canRunInParallel": false,
      "estimatedDuration": 60000
    },
    {
      "order": 2,
      "agentId": "agent-2",
      "task": "Refactor authentication code",
      "dependsOn": ["step-1"],
      "canRunInParallel": false,
      "estimatedDuration": 180000
    },
    {
      "order": 3,
      "agentId": "agent-3",
      "task": "Write unit tests",
      "dependsOn": ["step-2"],
      "canRunInParallel": true,
      "estimatedDuration": 120000
    }
  ],
  "estimatedTotalDuration": 360000
}
```

#### Execute Orchestration Plan
```http
POST /api/orchestration/plans/{planId}/execute
Authorization: Bearer {token}
```

**Response (202 Accepted):**
```json
{
  "executionId": "exec-456",
  "status": "Running",
  "startedAt": "2024-01-30T12:00:00Z"
}
```

#### Get Orchestration Status
```http
GET /api/orchestration/executions/{executionId}
Authorization: Bearer {token}
```

**Response (200 OK):**
```json
{
  "executionId": "exec-456",
  "planId": "plan-123",
  "status": "Running",
  "currentStep": 2,
  "totalSteps": 3,
  "progress": 66.67,
  "startedAt": "2024-01-30T12:00:00Z",
  "completedSteps": [
    {
      "stepId": "step-1",
      "status": "Completed",
      "duration": 58000,
      "output": "Analysis complete"
    }
  ],
  "currentAgent": "agent-2",
  "currentTask": "Refactoring authentication code"
}
```

### Tool Execution APIs

#### Execute Tool
```http
POST /api/agents/{agentId}/tools/execute
Authorization: Bearer {token}
```

**Request Body:**
```json
{
  "toolName": "ReadFile",
  "parameters": {
    "path": "C:\\Projects\\MyApp\\auth.js"
  },
  "sessionId": "session-abc123",
  "requireApproval": false
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "output": "File contents...",
  "executionTime": 150,
  "metadata": {
    "fileSize": 2048,
    "lineCount": 75
  }
}
```

#### List Available Tools
```http
GET /api/agents/{agentId}/tools
Authorization: Bearer {token}
```

**Response (200 OK):**
```json
{
  "tools": [
    {
      "name": "ReadFile",
      "description": "Read contents of a file",
      "requiresApproval": false,
      "schema": {
        "type": "object",
        "properties": {
          "path": {
            "type": "string",
            "description": "File path to read"
          }
        },
        "required": ["path"]
      }
    }
  ]
}
```

### Snapshot APIs

#### Create Snapshot
```http
POST /api/sessions/{sessionId}/snapshots
Authorization: Bearer {token}
```

**Request Body:**
```json
{
  "name": "Before refactoring",
  "description": "Snapshot before major changes"
}
```

**Response (200 OK):**
```json
{
  "snapshotId": "snap-789",
  "sessionId": "session-abc123",
  "name": "Before refactoring",
  "createdAt": "2024-01-30T12:00:00Z",
  "messageCount": 42,
  "size": 102400
}
```

#### Restore from Snapshot
```http
POST /api/snapshots/{snapshotId}/restore
Authorization: Bearer {token}
```

**Response (200 OK):**
```json
{
  "newSessionId": "session-def456",
  "name": "Development Session (Restored)",
  "restoredFrom": "snap-789",
  "messageCount": 42
}
```

### Configuration APIs

#### Get Application Configuration
```http
GET /api/configuration
Authorization: Bearer {token}
```

**Response (200 OK):**
```json
{
  "database": {
    "provider": "SQLite",
    "connectionString": "Data Source=orchestrator.db"
  },
  "signalr": {
    "keepAliveInterval": 15,
    "clientTimeoutInterval": 30,
    "maxMessageSize": 102400
  },
  "agents": {
    "claudeExecutablePath": "claude",
    "saturnLibraryPath": "./lib/saturn",
    "maxConcurrentAgents": 10,
    "defaultTimeout": 300000
  },
  "security": {
    "requireAuthentication": true,
    "tokenExpirationMinutes": 60
  }
}
```

#### Update Configuration
```http
PUT /api/configuration/{section}
Authorization: Bearer {token}
```

**Request Body:**
```json
{
  "maxConcurrentAgents": 20,
  "defaultTimeout": 600000
}
```

### MCP Configuration APIs

#### Get MCP Configuration
```http
GET /api/mcp/configuration
Authorization: Bearer {token}
```

**Response (200 OK):**
```json
{
  "globalTools": [
    {
      "name": "filesystem",
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-filesystem"],
      "enabled": true
    }
  ],
  "projectConfigurations": {
    "project-123": {
      "tools": [
        {
          "name": "project-tool",
          "command": "./tools/custom-tool",
          "env": {
            "PROJECT_PATH": "C:\\Projects\\MyApp"
          }
        }
      ]
    }
  }
}
```

#### Import MCP from Claude Desktop
```http
POST /api/mcp/import/claude-desktop
Authorization: Bearer {token}
```

**Response (200 OK):**
```json
{
  "imported": true,
  "toolsImported": 5,
  "configurations": [
    "filesystem",
    "github",
    "memory",
    "puppeteer",
    "slack"
  ]
}
```

## SignalR Hub Contracts

### OrchestratorHub Methods

#### Server-to-Client Methods
```typescript
interface IOrchestratorClient {
  // Connection events
  Connected(info: ConnectionInfo): Promise<void>;
  Disconnected(reason: string): Promise<void>;
  
  // Session events
  SessionCreated(session: Session): Promise<void>;
  SessionJoined(session: Session): Promise<void>;
  SessionUpdated(session: Session): Promise<void>;
  SessionEnded(sessionId: string): Promise<void>;
  
  // Orchestration events
  OrchestrationPlanCreated(plan: OrchestrationPlan): Promise<void>;
  OrchestrationProgress(progress: OrchestrationProgress): Promise<void>;
  OrchestrationCompleted(result: OrchestrationResult): Promise<void>;
  OrchestrationError(error: OrchestrationError): Promise<void>;
  
  // Error handling
  ReceiveError(error: ErrorResponse): Promise<void>;
}
```

#### Client-to-Server Methods
```typescript
interface IOrchestratorHub {
  // Session management
  CreateSession(request: CreateSessionRequest): Promise<SessionCreatedResponse>;
  JoinSession(sessionId: string): Promise<void>;
  LeaveSession(sessionId: string): Promise<void>;
  EndSession(sessionId: string): Promise<void>;
  
  // Orchestration
  SendOrchestrationMessage(request: OrchestrationMessageRequest): Promise<void>;
  CreateOrchestrationPlan(request: CreatePlanRequest): Promise<OrchestrationPlan>;
  ExecutePlan(planId: string): Promise<void>;
  CancelExecution(executionId: string): Promise<void>;
  
  // Session operations
  CreateSnapshot(sessionId: string, name: string): Promise<SnapshotCreatedResponse>;
  RestoreSnapshot(snapshotId: string): Promise<SessionCreatedResponse>;
}
```

### AgentHub Methods

#### Server-to-Client Methods
```typescript
interface IAgentClient {
  // Agent responses
  ReceiveAgentResponse(response: AgentResponseDto): Promise<void>;
  ReceiveStreamingUpdate(update: StreamingUpdate): Promise<void>;
  
  // Agent status
  AgentStatusUpdate(status: AgentStatusDto): Promise<void>;
  AgentConnected(agentInfo: AgentInfo): Promise<void>;
  AgentDisconnected(agentId: string): Promise<void>;
  
  // Tool execution
  ToolExecutionStarted(update: ToolExecutionUpdate): Promise<void>;
  ToolExecutionUpdate(update: ToolExecutionUpdate): Promise<void>;
  ToolExecutionCompleted(result: ToolExecutionResult): Promise<void>;
  ToolApprovalRequired(request: ToolApprovalRequest): Promise<void>;
  
  // Errors
  ReceiveError(error: ErrorResponse): Promise<void>;
}
```

#### Client-to-Server Methods
```typescript
interface IAgentHub {
  // Agent communication
  SendAgentMessage(request: AgentMessageRequest): Promise<void>;
  StreamMessage(request: StreamMessageRequest): IAsyncEnumerable<AgentResponse>;
  
  // Agent management
  CreateAgent(config: AgentConfiguration): Promise<string>;
  SubscribeToAgent(agentId: string): Promise<void>;
  UnsubscribeFromAgent(agentId: string): Promise<void>;
  
  // Tool execution
  ExecuteTool(request: ToolExecutionRequest): Promise<ToolExecutionResponse>;
  ApproveTool(approvalId: string, approved: boolean): Promise<void>;
  
  // Agent control
  PauseAgent(agentId: string): Promise<void>;
  ResumeAgent(agentId: string): Promise<void>;
  RestartAgent(agentId: string): Promise<void>;
  ShutdownAgent(agentId: string): Promise<void>;
}
```

## Data Transfer Objects

### Common DTOs

```typescript
interface AgentInfo {
  id: string;
  name: string;
  type: AgentType;
  status: AgentStatus;
  description: string;
  capabilities: AgentCapabilities;
  workingDirectory: string;
}

interface Session {
  id: string;
  name: string;
  type: SessionType;
  status: SessionStatus;
  createdAt: Date;
  lastActivityAt: Date;
  participantAgents: AgentInfo[];
  messageCount: number;
  workingDirectory: string;
  projectId: string;
}

interface AgentMessage {
  id: string;
  content: string;
  role: MessageRole;
  agentId?: string;
  sessionId: string;
  timestamp: Date;
  attachments?: Attachment[];
  metadata?: Record<string, any>;
}

interface AgentResponse {
  messageId: string;
  content: string;
  type: ResponseType;
  isComplete: boolean;
  toolCalls?: ToolCall[];
  usage?: TokenUsage;
  metadata?: Record<string, any>;
}

interface ToolCall {
  id: string;
  toolName: string;
  parameters: Record<string, any>;
  agentId: string;
  sessionId: string;
}

interface OrchestrationPlan {
  id: string;
  sessionId: string;
  goal: string;
  strategy: OrchestrationStrategy;
  steps: OrchestrationStep[];
  sharedContext: Record<string, any>;
  requiredAgents: string[];
}

interface OrchestrationStep {
  id: string;
  order: number;
  agentId: string;
  task: string;
  dependsOn: string[];
  input: Record<string, any>;
  timeout: number;
  canRunInParallel: boolean;
}
```

## Error Responses

### Standard Error Format
```json
{
  "error": {
    "code": "AGENT_NOT_FOUND",
    "message": "Agent with ID 'agent-xyz' not found",
    "details": {
      "agentId": "agent-xyz",
      "timestamp": "2024-01-30T12:00:00Z"
    }
  },
  "traceId": "trace-123456"
}
```

### Error Codes
| Code | HTTP Status | Description |
|------|------------|-------------|
| INVALID_REQUEST | 400 | Request validation failed |
| UNAUTHORIZED | 401 | Authentication required |
| FORBIDDEN | 403 | Insufficient permissions |
| NOT_FOUND | 404 | Resource not found |
| CONFLICT | 409 | Resource conflict |
| AGENT_ERROR | 500 | Agent execution error |
| ORCHESTRATION_ERROR | 500 | Orchestration failure |
| INTERNAL_ERROR | 500 | Internal server error |
| SERVICE_UNAVAILABLE | 503 | Service temporarily unavailable |

## Authentication

### JWT Token Structure
```json
{
  "sub": "user-123",
  "name": "John Doe",
  "roles": ["user", "admin"],
  "exp": 1706619600,
  "iat": 1706616000,
  "nbf": 1706616000
}
```

### Authorization Headers
```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

## Rate Limiting

### Headers
```http
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1706620000
```

### Rate Limit Response (429)
```json
{
  "error": {
    "code": "RATE_LIMIT_EXCEEDED",
    "message": "Rate limit exceeded. Please retry after 60 seconds.",
    "retryAfter": 60
  }
}
```

## Versioning

### API Version Header
```http
API-Version: 1.0
```

### Deprecated Endpoint Response
```http
Deprecation: true
Sunset: 2024-12-31
Link: <https://api.orchestrator.com/v2/sessions>; rel="successor-version"
```

## Next Steps
1. Generate OpenAPI specification
2. Create Postman collection
3. Set up API documentation site
4. Implement request validation
5. Add integration tests
6. Create client SDKs

## Version History
- v1.0 - Initial API specification
- Date: 2024-01-30
- Status: Ready for implementation