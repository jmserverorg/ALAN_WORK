using ALAN.Agent.Services;
using ALAN.Shared.Models;
using ALAN.Shared.Services.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Agents.AI;
using Moq;

namespace ALAN.Agent.Tests.Services;

public class AutonomousAgentTests
{
    private readonly AutonomousAgent _agent;

    public AutonomousAgentTests()
    {
        // Create mocks for all dependencies
        var mockAIAgent = new Mock<AIAgent>();
        var mockLogger = new Mock<ILogger<AutonomousAgent>>();
        var mockLongTermMemory = new Mock<ILongTermMemoryService>();
        var mockShortTermMemory = new Mock<IShortTermMemoryService>();
        var mockConsolidation = new Mock<IMemoryConsolidationService>();
        
        // Create real service instances with mocked dependencies
        var stateManager = new StateManager(mockShortTermMemory.Object, mockLongTermMemory.Object);
        var usageTracker = new UsageTracker(Mock.Of<ILogger<UsageTracker>>(), 4000, 8000000);
        var batchLearning = new BatchLearningService(
            mockConsolidation.Object,
            mockLongTermMemory.Object,
            Mock.Of<ILogger<BatchLearningService>>());
        var humanInput = new HumanInputHandler(
            Mock.Of<ILogger<HumanInputHandler>>(),
            stateManager);

        _agent = new AutonomousAgent(
            mockAIAgent.Object,
            mockLogger.Object,
            stateManager,
            usageTracker,
            mockLongTermMemory.Object,
            mockShortTermMemory.Object,
            batchLearning,
            humanInput);
    }

    [Fact]
    public void BuildMemoryContext_WithEmptyMemories_ReturnsDefaultMessage()
    {
        // Arrange
        _agent.SetRecentMemoriesForTesting(new List<MemoryEntry>());

        // Act
        var result = _agent.BuildMemoryContext();

        // Assert
        Assert.Equal("No previous memories available yet. This may be your first iteration.", result);
    }

    [Fact]
    public void BuildMemoryContext_GroupsByMemoryType()
    {
        // Arrange
        var memories = new List<MemoryEntry>
        {
            new MemoryEntry
            {
                Type = MemoryType.Learning,
                Summary = "Learning 1",
                Importance = 0.9,
                Timestamp = DateTime.UtcNow.AddHours(-2)
            },
            new MemoryEntry
            {
                Type = MemoryType.Success,
                Summary = "Success 1",
                Importance = 0.8,
                Timestamp = DateTime.UtcNow.AddHours(-1)
            }
        };
        _agent.SetRecentMemoriesForTesting(memories);

        // Act
        var result = _agent.BuildMemoryContext();

        // Assert
        Assert.Contains("### Learning", result);
        Assert.Contains("### Success", result);
        Assert.Contains("Learning 1", result);
        Assert.Contains("Success 1", result);
    }

    [Fact]
    public void BuildMemoryContext_OrdersByTypePriority()
    {
        // Arrange
        var memories = new List<MemoryEntry>
        {
            new MemoryEntry
            {
                Type = MemoryType.Decision,
                Summary = "Decision 1",
                Importance = 0.9,
                Timestamp = DateTime.UtcNow
            },
            new MemoryEntry
            {
                Type = MemoryType.Learning,
                Summary = "Learning 1",
                Importance = 0.9,
                Timestamp = DateTime.UtcNow
            },
            new MemoryEntry
            {
                Type = MemoryType.Reflection,
                Summary = "Reflection 1",
                Importance = 0.9,
                Timestamp = DateTime.UtcNow
            },
            new MemoryEntry
            {
                Type = MemoryType.Success,
                Summary = "Success 1",
                Importance = 0.9,
                Timestamp = DateTime.UtcNow
            }
        };
        _agent.SetRecentMemoriesForTesting(memories);

        // Act
        var result = _agent.BuildMemoryContext();

        // Assert
        // Learning (priority 5) should appear before Reflection (4), Success (3), and Decision (2)
        var learningIndex = result.IndexOf("### Learning");
        var reflectionIndex = result.IndexOf("### Reflection");
        var successIndex = result.IndexOf("### Success");
        var decisionIndex = result.IndexOf("### Decision");

        Assert.True(learningIndex < reflectionIndex, "Learning should appear before Reflection");
        Assert.True(reflectionIndex < successIndex, "Reflection should appear before Success");
        Assert.True(successIndex < decisionIndex, "Success should appear before Decision");
    }

    [Fact]
    public void BuildMemoryContext_FormatsAgeCorrectly_Hours()
    {
        // Arrange
        var memories = new List<MemoryEntry>
        {
            new MemoryEntry
            {
                Type = MemoryType.Learning,
                Summary = "Recent learning",
                Importance = 0.9,
                Timestamp = DateTime.UtcNow.AddHours(-5)
            }
        };
        _agent.SetRecentMemoriesForTesting(memories);

        // Act
        var result = _agent.BuildMemoryContext();

        // Assert
        Assert.Contains("5h ago", result);
    }

    [Fact]
    public void BuildMemoryContext_FormatsAgeCorrectly_Days()
    {
        // Arrange
        var memories = new List<MemoryEntry>
        {
            new MemoryEntry
            {
                Type = MemoryType.Learning,
                Summary = "Old learning",
                Importance = 0.9,
                Timestamp = DateTime.UtcNow.AddDays(-3)
            }
        };
        _agent.SetRecentMemoriesForTesting(memories);

        // Act
        var result = _agent.BuildMemoryContext();

        // Assert
        Assert.Contains("3d ago", result);
    }

    [Fact]
    public void BuildMemoryContext_IncludesImportanceScore()
    {
        // Arrange
        var memories = new List<MemoryEntry>
        {
            new MemoryEntry
            {
                Type = MemoryType.Learning,
                Summary = "Important learning",
                Importance = 0.85,
                Timestamp = DateTime.UtcNow.AddHours(-1)
            }
        };
        _agent.SetRecentMemoriesForTesting(memories);

        // Act
        var result = _agent.BuildMemoryContext();

        // Assert
        Assert.Contains("importance: 0.85", result);
    }

    [Fact]
    public void BuildMemoryContext_IncludesFullContentForHighImportance()
    {
        // Arrange
        var memories = new List<MemoryEntry>
        {
            new MemoryEntry
            {
                Type = MemoryType.Learning,
                Summary = "High importance learning",
                Content = "This is the detailed content that should be included for high importance items.",
                Importance = 0.85, // >= 0.8 threshold
                Timestamp = DateTime.UtcNow.AddHours(-1)
            }
        };
        _agent.SetRecentMemoriesForTesting(memories);

        // Act
        var result = _agent.BuildMemoryContext();

        // Assert
        Assert.Contains("Details:", result);
        Assert.Contains("This is the detailed content", result);
    }

    [Fact]
    public void BuildMemoryContext_ExcludesContentForLowImportance()
    {
        // Arrange
        var memories = new List<MemoryEntry>
        {
            new MemoryEntry
            {
                Type = MemoryType.Learning,
                Summary = "Low importance learning",
                Content = "This detailed content should not be included for low importance items.",
                Importance = 0.5, // < 0.8 threshold
                Timestamp = DateTime.UtcNow.AddHours(-1)
            }
        };
        _agent.SetRecentMemoriesForTesting(memories);

        // Act
        var result = _agent.BuildMemoryContext();

        // Assert
        Assert.DoesNotContain("Details:", result);
        Assert.DoesNotContain("This detailed content should not be included", result);
        Assert.Contains("Low importance learning", result);
    }

    [Fact]
    public void BuildMemoryContext_TruncatesContentAt200Characters()
    {
        // Arrange
        var longContent = new string('x', 250); // 250 characters
        var memories = new List<MemoryEntry>
        {
            new MemoryEntry
            {
                Type = MemoryType.Learning,
                Summary = "Learning with long content",
                Content = longContent,
                Importance = 0.85,
                Timestamp = DateTime.UtcNow.AddHours(-1)
            }
        };
        _agent.SetRecentMemoriesForTesting(memories);

        // Act
        var result = _agent.BuildMemoryContext();

        // Assert
        Assert.Contains("Details:", result);
        // Should contain exactly 200 chars + "..."
        Assert.Contains(new string('x', 200) + "...", result);
        // Should not contain the full 250 character string
        Assert.DoesNotContain(longContent, result);
    }

    [Fact]
    public void BuildMemoryContext_DoesNotTruncateShortContent()
    {
        // Arrange
        var shortContent = "This is a short content that should not be truncated.";
        var memories = new List<MemoryEntry>
        {
            new MemoryEntry
            {
                Type = MemoryType.Learning,
                Summary = "Learning with short content",
                Content = shortContent,
                Importance = 0.85,
                Timestamp = DateTime.UtcNow.AddHours(-1)
            }
        };
        _agent.SetRecentMemoriesForTesting(memories);

        // Act
        var result = _agent.BuildMemoryContext();

        // Assert
        Assert.Contains(shortContent, result);
        // Should not have "..." since content is short
        var detailsLine = result.Split('\n').FirstOrDefault(line => line.Contains("Details:"));
        Assert.NotNull(detailsLine);
        Assert.DoesNotContain("...", detailsLine);
    }

    [Fact]
    public void BuildMemoryContext_TakesOnlyTop5PerGroup()
    {
        // Arrange
        var memories = new List<MemoryEntry>();
        for (int i = 0; i < 10; i++)
        {
            memories.Add(new MemoryEntry
            {
                Type = MemoryType.Learning,
                Summary = $"Learning {i}",
                Importance = 0.9 - (i * 0.05), // Descending importance
                Timestamp = DateTime.UtcNow.AddHours(-i)
            });
        }
        _agent.SetRecentMemoriesForTesting(memories);

        // Act
        var result = _agent.BuildMemoryContext();

        // Assert
        // Should contain first 5 (highest importance)
        for (int i = 0; i < 5; i++)
        {
            Assert.Contains($"Learning {i}", result);
        }
        // Should NOT contain 6-9
        for (int i = 5; i < 10; i++)
        {
            Assert.DoesNotContain($"Learning {i}", result);
        }
    }

    [Fact]
    public void BuildMemoryContext_OrdersByImportanceWithinGroup()
    {
        // Arrange
        var memories = new List<MemoryEntry>
        {
            new MemoryEntry
            {
                Type = MemoryType.Learning,
                Summary = "Low importance",
                Importance = 0.5,
                Timestamp = DateTime.UtcNow
            },
            new MemoryEntry
            {
                Type = MemoryType.Learning,
                Summary = "High importance",
                Importance = 0.9,
                Timestamp = DateTime.UtcNow
            },
            new MemoryEntry
            {
                Type = MemoryType.Learning,
                Summary = "Medium importance",
                Importance = 0.7,
                Timestamp = DateTime.UtcNow
            }
        };
        _agent.SetRecentMemoriesForTesting(memories);

        // Act
        var result = _agent.BuildMemoryContext();

        // Assert
        var highIndex = result.IndexOf("High importance");
        var mediumIndex = result.IndexOf("Medium importance");
        var lowIndex = result.IndexOf("Low importance");

        Assert.True(highIndex < mediumIndex, "High importance should appear before medium");
        Assert.True(mediumIndex < lowIndex, "Medium importance should appear before low");
    }

    [Fact]
    public void BuildMemoryContext_HandlesContentSameAsSummary()
    {
        // Arrange
        var memories = new List<MemoryEntry>
        {
            new MemoryEntry
            {
                Type = MemoryType.Learning,
                Summary = "Same content",
                Content = "Same content", // Same as summary
                Importance = 0.85,
                Timestamp = DateTime.UtcNow.AddHours(-1)
            }
        };
        _agent.SetRecentMemoriesForTesting(memories);

        // Act
        var result = _agent.BuildMemoryContext();

        // Assert
        // Should not include Details section when content equals summary
        Assert.DoesNotContain("Details:", result);
        Assert.Contains("Same content", result);
    }

    [Fact]
    public void BuildMemoryContext_HandlesEmptyContent()
    {
        // Arrange
        var memories = new List<MemoryEntry>
        {
            new MemoryEntry
            {
                Type = MemoryType.Learning,
                Summary = "No content",
                Content = "", // Empty content
                Importance = 0.85,
                Timestamp = DateTime.UtcNow.AddHours(-1)
            }
        };
        _agent.SetRecentMemoriesForTesting(memories);

        // Act
        var result = _agent.BuildMemoryContext();

        // Assert
        // Should not include Details section when content is empty
        Assert.DoesNotContain("Details:", result);
        Assert.Contains("No content", result);
    }

    [Fact]
    public void BuildMemoryContext_HandlesNullContent()
    {
        // Arrange
        var memories = new List<MemoryEntry>
        {
            new MemoryEntry
            {
                Type = MemoryType.Learning,
                Summary = "Null content",
                Content = null!, // Null content
                Importance = 0.85,
                Timestamp = DateTime.UtcNow.AddHours(-1)
            }
        };
        _agent.SetRecentMemoriesForTesting(memories);

        // Act
        var result = _agent.BuildMemoryContext();

        // Assert
        // Should not include Details section when content is null
        Assert.DoesNotContain("Details:", result);
        Assert.Contains("Null content", result);
    }

    [Fact]
    public void BuildMemoryContext_ShowsCorrectEntryCount()
    {
        // Arrange
        var memories = new List<MemoryEntry>
        {
            new MemoryEntry { Type = MemoryType.Learning, Summary = "L1", Importance = 0.9, Timestamp = DateTime.UtcNow },
            new MemoryEntry { Type = MemoryType.Learning, Summary = "L2", Importance = 0.8, Timestamp = DateTime.UtcNow },
            new MemoryEntry { Type = MemoryType.Success, Summary = "S1", Importance = 0.7, Timestamp = DateTime.UtcNow }
        };
        _agent.SetRecentMemoriesForTesting(memories);

        // Act
        var result = _agent.BuildMemoryContext();

        // Assert
        Assert.Contains("### Learning (2 entries):", result);
        Assert.Contains("### Success (1 entries):", result);
    }
}
