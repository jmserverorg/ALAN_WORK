# Code Proposal System

ALAN's code proposal system ensures all autonomous code changes require human approval before implementation.

## Overview

The code proposal system provides a safe mechanism for the agent to:
1. Analyze code and identify potential improvements
2. Generate code change proposals
3. Submit proposals for human review
4. Implement approved changes via pull requests
5. Learn from approved and rejected proposals

## Proposal Lifecycle

```
Created (Pending) → Approved/Rejected
       ↓
   Approved → Implemented/Failed
       ↓
   Rejected → Stored as learning
```

## Creating a Proposal

The agent creates proposals through the `CodeProposalService`:

```csharp
var proposal = new CodeProposal
{
    Title = "Add error handling to API calls",
    Description = "Wrap API calls in try-catch blocks to handle network errors gracefully",
    Reasoning = "Analysis of recent errors shows 5 failures due to unhandled network exceptions",
    FileChanges = new List<FileChange>
    {
        new FileChange
        {
            FilePath = "src/Services/ApiService.cs",
            Type = ChangeType.Modify,
            OriginalContent = "// Original code",
            NewContent = "// Modified code with error handling",
            Diff = "// Unified diff"
        }
    }
};

var proposalId = await codeProposalService.CreateProposalAsync(proposal);
```

## Proposal Structure

```csharp
public class CodeProposal
{
    public string Id { get; set; }
    public DateTime Created { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Reasoning { get; set; }
    public List<FileChange> FileChanges { get; set; }
    public ProposalStatus Status { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    public string? BranchName { get; set; }
    public string? PullRequestUrl { get; set; }
}
```

## Reviewing Proposals

### REST API

```bash
# Get all pending proposals
GET /api/proposals?status=Pending

# Get a specific proposal
GET /api/proposals/{id}

# Approve a proposal
POST /api/proposals/{id}/approve
{
  "approvedBy": "user@example.com"
}

# Reject a proposal
POST /api/proposals/{id}/reject
{
  "reason": "This change could break backward compatibility"
}

# Get statistics
GET /api/proposals/statistics
```

### Using C#

```csharp
// Get pending proposals
var pending = codeProposalService.GetPendingProposals();

foreach (var proposal in pending)
{
    Console.WriteLine($"{proposal.Title}");
    Console.WriteLine($"Reasoning: {proposal.Reasoning}");
    
    // Review file changes
    foreach (var change in proposal.FileChanges)
    {
        Console.WriteLine($"File: {change.FilePath}");
        Console.WriteLine($"Type: {change.Type}");
        Console.WriteLine($"Diff:\n{change.Diff}");
    }
}

// Approve a proposal
await codeProposalService.ApproveProposalAsync(proposalId, "reviewer@example.com");

// Reject a proposal
await codeProposalService.RejectProposalAsync(
    proposalId,
    "This needs more testing before implementation"
);
```

## Implementing Approved Proposals

After approval, the proposal can be implemented:

1. Create a branch for the changes
2. Apply the file changes
3. Create a pull request
4. Mark proposal as implemented

```csharp
// After creating PR
await codeProposalService.MarkAsImplementedAsync(
    proposalId,
    "https://github.com/owner/repo/pull/123"
);
```

## Proposal Statistics

Track proposal metrics:

```csharp
var stats = codeProposalService.GetStatistics();

Console.WriteLine($"Total: {stats.Total}");
Console.WriteLine($"Pending: {stats.Pending}");
Console.WriteLine($"Approved: {stats.Approved}");
Console.WriteLine($"Rejected: {stats.Rejected}");
Console.WriteLine($"Implemented: {stats.Implemented}");
```

## Memory Integration

All proposals are stored in long-term memory:
- **Created** - Stored with "pending-approval" tag
- **Approved** - Stored with "approved" tag and approver info
- **Rejected** - Stored with rejection reason for learning
- **Implemented** - Stored with success tag and PR link

The agent learns from:
- Which types of changes are approved
- Common rejection reasons
- Successful implementation patterns
- Approval patterns by reviewer

## Safety Features

### Human-in-the-Loop
- All code changes require explicit human approval
- No automatic implementation of proposals
- Clear reasoning must be provided for each change

### Audit Trail
- All proposals logged with timestamps
- Approvals/rejections tracked with reasons
- Complete history in long-term memory
- Pull request links for implemented changes

### Validation
- File changes include diffs for review
- Original content preserved
- Reasoning required for each proposal
- Status transitions enforced (can't implement rejected proposals)

## Best Practices

### For the Agent
1. Provide clear, detailed reasoning
2. Keep changes small and focused
3. Include comprehensive diffs
4. Reference relevant memories or learnings
5. Explain expected benefits

### For Reviewers
1. Review reasoning carefully
2. Check file diffs for correctness
3. Consider impact on existing functionality
4. Provide clear rejection reasons
5. Approve incrementally

### For Implementation
1. Test changes before marking as implemented
2. Create descriptive pull requests
3. Link PR to original proposal
4. Monitor for issues after merge
5. Update proposal status promptly

## Example Workflow

```
1. Agent analyzes code
   └─> Identifies improvement opportunity
   
2. Agent creates proposal
   └─> Stored in memory with "pending-approval" tag
   
3. Human reviews proposal
   ├─> Approves ─> Proposal marked "Approved"
   │              └─> Agent creates PR
   │                  └─> PR merged
   │                      └─> Marked "Implemented"
   │
   └─> Rejects ─> Proposal marked "Rejected"
                  └─> Agent learns from rejection
```

## Integration with GitHub MCP

The code proposal system integrates with GitHub MCP for:
- Reading repository contents
- Creating branches
- Submitting pull requests
- Checking CI/CD status

```csharp
// Example: Create PR via GitHub MCP
var response = await mcpManager.InvokeToolAsync(
    "GitHub",
    "create_pull_request",
    new Dictionary<string, object>
    {
        ["owner"] = "organization",
        ["repo"] = "repository",
        ["title"] = proposal.Title,
        ["body"] = $"{proposal.Description}\n\n{proposal.Reasoning}",
        ["head"] = proposal.BranchName,
        ["base"] = "main"
    }
);

if (response.Success)
{
    var prUrl = response.Data["url"].ToString();
    await codeProposalService.MarkAsImplementedAsync(proposal.Id, prUrl);
}
```

## Future Enhancements

- Automated testing of proposals before submission
- Similarity detection to avoid duplicate proposals
- Learning from code review comments
- Automated PR creation workflow
- Integration with CI/CD for validation
- Code quality metrics for proposals
- A/B testing of proposed changes
