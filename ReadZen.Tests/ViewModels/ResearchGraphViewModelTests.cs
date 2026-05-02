using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ReadZen.App.Models;
using ReadZen.App.Services;
using ReadZen.App.ViewModels;
using Xunit;

namespace ReadZen.Tests.ViewModels;

public class ScholarNodeTypeTests
{
    [Fact]
    public void Book_EnumValue_Is5()
    {
        Assert.Equal(5, (int)ScholarNodeType.Book);
    }

    [Theory]
    [InlineData(ScholarNodeType.Passage, 0)]
    [InlineData(ScholarNodeType.Concept, 1)]
    [InlineData(ScholarNodeType.ZenMaster, 2)]
    [InlineData(ScholarNodeType.TermbaseEntry, 3)]
    [InlineData(ScholarNodeType.Collection, 4)]
    [InlineData(ScholarNodeType.Book, 5)]
    public void AllEnumValues_HaveExpectedIntValues(ScholarNodeType type, int expected)
    {
        Assert.Equal(expected, (int)type);
    }
}

public class EdgeTypeRegistryTests
{
    [Fact]
    public void GetValidTypes_PassageToPassage_Returns9Types()
    {
        var types = EdgeTypeRegistry.GetValidTypes(ScholarNodeType.Passage, ScholarNodeType.Passage);
        Assert.Equal(9, types.Count);
        Assert.Contains(types, t => t.Id == "quotes");
        Assert.Contains(types, t => t.Id == "parallels");
        Assert.Contains(types, t => t.Id == "summarizes");
    }

    [Fact]
    public void GetValidTypes_PassageToConcept_Returns2Types()
    {
        var types = EdgeTypeRegistry.GetValidTypes(ScholarNodeType.Passage, ScholarNodeType.Concept);
        Assert.Equal(2, types.Count);
        Assert.Contains(types, t => t.Id == "evidences");
        Assert.Contains(types, t => t.Id == "refutes");
    }

    [Fact]
    public void GetValidTypes_IncompatiblePair_ReturnsEmpty()
    {
        var types = EdgeTypeRegistry.GetValidTypes(ScholarNodeType.ZenMaster, ScholarNodeType.TermbaseEntry);
        Assert.Empty(types);
    }

    [Fact]
    public void GetById_ExistingType_ReturnsDefinition()
    {
        var def = EdgeTypeRegistry.GetById("quotes");
        Assert.NotNull(def);
        Assert.Equal("Quotes", def!.DisplayName);
        Assert.True(def.IsDirectional);
    }

    [Fact]
    public void GetById_NonExistent_ReturnsNull()
    {
        var def = EdgeTypeRegistry.GetById("nonexistent-type-xyz");
        Assert.Null(def);
    }

    [Fact]
    public void GetById_WithCustomTypes_FindsCustomEdge()
    {
        var custom = new EdgeTypeDefinition
        {
            Id = "my-custom-edge",
            DisplayName = "Custom Link",
            Description = "User-defined edge type",
            AllowedFromTypes = new() { ScholarNodeType.Passage },
            AllowedToTypes = new() { ScholarNodeType.Passage },
            ColorHex = "#FF0000",
            IsBuiltIn = false,
            IsDirectional = true
        };

        var result = EdgeTypeRegistry.GetById("my-custom-edge", new[] { custom });

        Assert.NotNull(result);
        Assert.Equal("Custom Link", result!.DisplayName);
        Assert.False(result.IsBuiltIn);
    }

    [Fact]
    public void GetById_WithCustomTypes_BuiltInStillFound()
    {
        var custom = new EdgeTypeDefinition
        {
            Id = "my-custom-edge",
            DisplayName = "Custom",
            IsBuiltIn = false
        };

        var result = EdgeTypeRegistry.GetById("quotes", new[] { custom });

        Assert.NotNull(result);
        Assert.Equal("Quotes", result!.DisplayName);
        Assert.True(result.IsBuiltIn);
    }

    [Fact]
    public void GetById_WithCustomTypes_NullCustomTypes_ReturnsBuiltIn()
    {
        var result = EdgeTypeRegistry.GetById("quotes", null);

        Assert.NotNull(result);
        Assert.Equal("Quotes", result!.DisplayName);
    }

    [Fact]
    public void GetById_CustomTypeNotInBuiltIn_NullCustomTypes_ReturnsNull()
    {
        var result = EdgeTypeRegistry.GetById("my-custom-edge", null);

        Assert.Null(result);
    }

    // --- Book-related edge type tests ---

    [Fact]
    public void GetValidTypes_PassageToBook_ReturnsBookEdgeTypes()
    {
        var types = EdgeTypeRegistry.GetValidTypes(ScholarNodeType.Passage, ScholarNodeType.Book);
        Assert.True(types.Count >= 2, $"Expected at least 2 Passage->Book types, got {types.Count}");
        Assert.Contains(types, t => t.Id == "excerpted-from-book");
        Assert.Contains(types, t => t.Id == "appears-in");
    }

    [Fact]
    public void GetValidTypes_BookToPassage_ReturnsBookContains()
    {
        var types = EdgeTypeRegistry.GetValidTypes(ScholarNodeType.Book, ScholarNodeType.Passage);
        Assert.Contains(types, t => t.Id == "book-contains");
    }

    [Fact]
    public void GetValidTypes_BookToConcept_ReturnsBookExplores()
    {
        var types = EdgeTypeRegistry.GetValidTypes(ScholarNodeType.Book, ScholarNodeType.Concept);
        Assert.Contains(types, t => t.Id == "book-explores");
    }

    [Fact]
    public void GetValidTypes_BookToZenMaster_ReturnsAttributionTypes()
    {
        var types = EdgeTypeRegistry.GetValidTypes(ScholarNodeType.Book, ScholarNodeType.ZenMaster);
        Assert.Contains(types, t => t.Id == "book-attributed-to");
        Assert.Contains(types, t => t.Id == "book-records");
    }

    [Fact]
    public void GetValidTypes_BookToBook_ReturnsRelationTypes()
    {
        var types = EdgeTypeRegistry.GetValidTypes(ScholarNodeType.Book, ScholarNodeType.Book);
        Assert.Contains(types, t => t.Id == "related-book");
        Assert.Contains(types, t => t.Id == "commentary-on-book");
    }

    [Fact]
    public void GetValidTypes_BookToCollection_ReturnsBookInCollection()
    {
        var types = EdgeTypeRegistry.GetValidTypes(ScholarNodeType.Book, ScholarNodeType.Collection);
        Assert.Contains(types, t => t.Id == "book-in-collection");
    }

    [Fact]
    public void GetValidTypes_ZenMasterToBook_ReturnsMasterAuthoredBook()
    {
        var types = EdgeTypeRegistry.GetValidTypes(ScholarNodeType.ZenMaster, ScholarNodeType.Book);
        Assert.Contains(types, t => t.Id == "master-authored-book");
    }

    [Fact]
    public void BookEdgeTypes_HaveCorrectAllowedTypes()
    {
        var excerptedFromBook = EdgeTypeRegistry.GetById("excerpted-from-book");
        Assert.NotNull(excerptedFromBook);
        Assert.Contains(ScholarNodeType.Passage, excerptedFromBook!.AllowedFromTypes);
        Assert.Contains(ScholarNodeType.Book, excerptedFromBook.AllowedToTypes);
        Assert.True(excerptedFromBook.IsDirectional);

        var bookContains = EdgeTypeRegistry.GetById("book-contains");
        Assert.NotNull(bookContains);
        Assert.Contains(ScholarNodeType.Book, bookContains!.AllowedFromTypes);
        Assert.Contains(ScholarNodeType.Passage, bookContains.AllowedToTypes);

        var relatedBook = EdgeTypeRegistry.GetById("related-book");
        Assert.NotNull(relatedBook);
        Assert.Contains(ScholarNodeType.Book, relatedBook!.AllowedFromTypes);
        Assert.Contains(ScholarNodeType.Book, relatedBook.AllowedToTypes);
        Assert.False(relatedBook.IsDirectional);
    }

    [Fact]
    public void BookEdgeTypes_HaveExpectedColorHex()
    {
        // Most book edge types use the book-brown "#D4A574"
        var brownBookEdgeIds = new[]
        {
            "excerpted-from-book", "appears-in", "book-contains", "book-explores",
            "book-attributed-to", "book-records", "related-book",
            "master-authored-book"
        };
        foreach (var id in brownBookEdgeIds)
        {
            var def = EdgeTypeRegistry.GetById(id);
            Assert.NotNull(def);
            Assert.Equal("#D4A574", def!.ColorHex);
        }

        // book-in-collection uses the collection purple
        var bookInCollection = EdgeTypeRegistry.GetById("book-in-collection");
        Assert.NotNull(bookInCollection);
        Assert.Equal("#AB47BC", bookInCollection!.ColorHex);
    }

    [Fact]
    public void CommentaryOnBook_IsDirectional()
    {
        var def = EdgeTypeRegistry.GetById("commentary-on-book");
        Assert.NotNull(def);
        Assert.True(def!.IsDirectional);
        // Commentary uses green, not book-brown
        Assert.Equal("#51D996", def.ColorHex);
    }
}

public class ResearchGraphViewModelTests
{
    private static ScholarCollection CreateTestCollection(int passageCount = 2, int conceptCount = 1)
    {
        var collection = new ScholarCollection
        {
            Id = "test-col",
            Name = "Test Collection",
            SchemaVersion = 2
        };

        for (int i = 0; i < passageCount; i++)
        {
            collection.Passages.Add(new ScholarPassage
            {
                Id = $"p{i}",
                ZhText = $"Chinese text {i}",
                EnText = $"English text {i}",
                MasterNames = new List<string>()
            });
        }

        for (int i = 0; i < conceptCount; i++)
        {
            collection.Concepts.Add(new ConceptNode
            {
                Id = $"c{i}",
                Name = $"Concept {i}",
                Description = "A test concept"
            });
        }

        return collection;
    }

    private static ResearchGraphViewModel CreateVm(ScholarCollection? collection = null)
    {
        collection ??= CreateTestCollection();
        return new ResearchGraphViewModel(collection, new List<ScholarCollection> { collection });
    }

    [Fact]
    public void Constructor_WithPassages_CreatesNodes()
    {
        var collection = CreateTestCollection(passageCount: 3, conceptCount: 0);
        var vm = CreateVm(collection);

        // 3 passage nodes
        var passageNodes = vm.Nodes.Where(n => n.NodeType == ScholarNodeType.Passage).ToList();
        Assert.Equal(3, passageNodes.Count);
        Assert.All(passageNodes, n => Assert.False(string.IsNullOrEmpty(n.NodeId)));
    }

    [Fact]
    public void Constructor_WithEdges_CreatesEdgeVms()
    {
        var collection = CreateTestCollection(passageCount: 2, conceptCount: 0);
        collection.Edges.Add(new ScholarGraphEdge
        {
            Id = "e1",
            FromNodeId = "p0",
            FromNodeType = ScholarNodeType.Passage,
            ToNodeId = "p1",
            ToNodeType = ScholarNodeType.Passage,
            RelationType = "quotes"
        });

        var vm = CreateVm(collection);

        Assert.Single(vm.Edges);
        Assert.Equal("e1", vm.Edges[0].EdgeId);
        Assert.Equal("p0", vm.Edges[0].From.NodeId);
        Assert.Equal("p1", vm.Edges[0].To.NodeId);
    }

    [Fact]
    public void EdgeVm_Label_UsesDisplayNameNotRelationType()
    {
        var collection = CreateTestCollection(passageCount: 2, conceptCount: 0);
        collection.Edges.Add(new ScholarGraphEdge
        {
            Id = "e-label",
            FromNodeId = "p0",
            FromNodeType = ScholarNodeType.Passage,
            ToNodeId = "p1",
            ToNodeType = ScholarNodeType.Passage,
            RelationType = "quotes"
        });

        var vm = CreateVm(collection);

        var edgeVm = vm.Edges.First(e => e.EdgeId == "e-label");
        // Label should be the human-readable DisplayName ("Quotes"), not the raw id ("quotes")
        Assert.Equal("Quotes", edgeVm.Label);
        Assert.Equal("quotes", edgeVm.RelationType);
    }

    [Fact]
    public void EdgeVm_Label_FallsBackToRelationTypeWhenUnknown()
    {
        var collection = CreateTestCollection(passageCount: 2, conceptCount: 0);
        collection.Edges.Add(new ScholarGraphEdge
        {
            Id = "e-unknown",
            FromNodeId = "p0",
            FromNodeType = ScholarNodeType.Passage,
            ToNodeId = "p1",
            ToNodeType = ScholarNodeType.Passage,
            RelationType = "unknown-relation-xyz"
        });

        var vm = CreateVm(collection);

        var edgeVm = vm.Edges.First(e => e.EdgeId == "e-unknown");
        // When EdgeTypeRegistry doesn't know the type, Label falls back to RelationType
        Assert.Equal("unknown-relation-xyz", edgeVm.Label);
    }

    [Fact]
    public void AddEdge_Label_UsesDisplayNameFromRegistry()
    {
        var collection = CreateTestCollection(passageCount: 2, conceptCount: 0);
        var vm = CreateVm(collection);

        vm.AddEdge(new ScholarGraphEdge
        {
            Id = "e-add-label",
            FromNodeId = "p0",
            ToNodeId = "p1",
            RelationType = "comments-on"
        });

        var edgeVm = vm.Edges.First(e => e.EdgeId == "e-add-label");
        Assert.Equal("Comments on", edgeVm.Label);
    }

    [Fact]
    public void AddConcept_CreatesNodeAndAddsToCollection()
    {
        var vm = CreateVm();
        var concept = new ConceptNode { Id = "new-c", Name = "New Concept", Description = "desc" };

        vm.AddConcept(concept);

        Assert.Contains(vm.Nodes, n => n.NodeId == "new-c" && n.NodeType == ScholarNodeType.Concept);
        Assert.Contains(vm.GetCollection().Concepts, c => c.Id == "new-c");
    }

    [Fact]
    public void AddEdge_ConnectsNodes_IncrementsDegree()
    {
        var collection = CreateTestCollection(passageCount: 2, conceptCount: 0);
        var vm = CreateVm(collection);

        var fromNode = vm.Nodes.First(n => n.NodeId == "p0");
        var toNode = vm.Nodes.First(n => n.NodeId == "p1");
        int fromDegreeBefore = fromNode.Degree;
        int toDegreeBefore = toNode.Degree;

        vm.AddEdge(new ScholarGraphEdge
        {
            Id = "e1",
            FromNodeId = "p0",
            ToNodeId = "p1",
            RelationType = "quotes"
        });

        Assert.Equal(fromDegreeBefore + 1, fromNode.Degree);
        Assert.Equal(toDegreeBefore + 1, toNode.Degree);
        Assert.Single(vm.Edges);
    }

    [Fact]
    public void AddEdge_SelfLoop_IsBlocked()
    {
        var vm = CreateVm();

        vm.AddEdge(new ScholarGraphEdge
        {
            Id = "self",
            FromNodeId = "p0",
            ToNodeId = "p0",
            RelationType = "quotes"
        });

        Assert.Empty(vm.Edges);
    }

    [Fact]
    public void RemoveNode_RemovesFromNodesAndCollection()
    {
        var collection = CreateTestCollection(passageCount: 2, conceptCount: 1);
        var vm = CreateVm(collection);

        vm.RemoveNode("c0");

        Assert.DoesNotContain(vm.Nodes, n => n.NodeId == "c0");
        Assert.DoesNotContain(vm.GetCollection().Concepts, c => c.Id == "c0");
    }

    [Fact]
    public void RemoveNode_RemovesConnectedEdges()
    {
        var collection = CreateTestCollection(passageCount: 2, conceptCount: 1);
        collection.Edges.Add(new ScholarGraphEdge
        {
            Id = "e1",
            FromNodeId = "p0",
            ToNodeId = "c0",
            RelationType = "evidences"
        });
        var vm = CreateVm(collection);

        Assert.Single(vm.Edges);

        vm.RemoveNode("c0");

        Assert.Empty(vm.Edges);
    }

    [Fact]
    public void RemoveNode_DecrementsDegreeOnConnectedNodes()
    {
        var collection = CreateTestCollection(passageCount: 2, conceptCount: 1);
        collection.Edges.Add(new ScholarGraphEdge
        {
            Id = "e1",
            FromNodeId = "p0",
            ToNodeId = "c0",
            RelationType = "evidences"
        });
        var vm = CreateVm(collection);

        var p0 = vm.Nodes.First(n => n.NodeId == "p0");
        Assert.Equal(1, p0.Degree);

        vm.RemoveNode("c0");

        Assert.Equal(0, p0.Degree);
    }

    [Fact]
    public void SetEgoMode_DimsUnconnectedNodes()
    {
        var collection = CreateTestCollection(passageCount: 3, conceptCount: 0);
        collection.Edges.Add(new ScholarGraphEdge
        {
            Id = "e1",
            FromNodeId = "p0",
            ToNodeId = "p1",
            RelationType = "quotes"
        });
        var vm = CreateVm(collection);

        vm.SetEgoMode("p0");

        var p0 = vm.Nodes.First(n => n.NodeId == "p0");
        var p1 = vm.Nodes.First(n => n.NodeId == "p1");
        var p2 = vm.Nodes.First(n => n.NodeId == "p2");

        Assert.False(p0.IsDimmed); // ego node
        Assert.False(p1.IsDimmed); // connected
        Assert.True(p2.IsDimmed);  // not connected
    }

    [Fact]
    public void SetEgoMode_NonExistentId_NoOp()
    {
        var vm = CreateVm();
        // Should not throw
        vm.SetEgoMode("does-not-exist");

        // All nodes unchanged (not dimmed by default)
        Assert.All(vm.Nodes, n => Assert.False(n.IsDimmed));
    }

    [Fact]
    public void SetEgoMode_Null_ClearsDimming()
    {
        var collection = CreateTestCollection(passageCount: 3, conceptCount: 0);
        collection.Edges.Add(new ScholarGraphEdge
        {
            Id = "e1",
            FromNodeId = "p0",
            ToNodeId = "p1",
            RelationType = "quotes"
        });
        var vm = CreateVm(collection);

        vm.SetEgoMode("p0"); // dims p2
        vm.SetEgoMode(null); // clears all dimming

        Assert.All(vm.Nodes, n => Assert.False(n.IsDimmed));
    }

    [Fact]
    public void GetVisibleNodes_RespectsFilters()
    {
        var collection = CreateTestCollection(passageCount: 2, conceptCount: 1);
        var vm = CreateVm(collection);

        vm.ShowPassages = false;
        var visible = vm.GetVisibleNodes();

        Assert.DoesNotContain(visible, n => n.NodeType == ScholarNodeType.Passage);
        Assert.Contains(visible, n => n.NodeType == ScholarNodeType.Concept);
    }

    [Fact]
    public void GetVisibleEdges_HidesEdgesWithHiddenNodes()
    {
        var collection = CreateTestCollection(passageCount: 2, conceptCount: 1);
        collection.Edges.Add(new ScholarGraphEdge
        {
            Id = "e1",
            FromNodeId = "p0",
            ToNodeId = "c0",
            RelationType = "evidences"
        });
        var vm = CreateVm(collection);

        Assert.Single(vm.GetVisibleEdges());

        vm.ShowConcepts = false;
        Assert.Empty(vm.GetVisibleEdges());
    }

    [Fact]
    public void ComputeStats_CalculatesOrphans()
    {
        var collection = CreateTestCollection(passageCount: 3, conceptCount: 1);
        // Only connect p0 to c0; p1 and p2 are orphans
        collection.Edges.Add(new ScholarGraphEdge
        {
            Id = "e1",
            FromNodeId = "p0",
            ToNodeId = "c0",
            RelationType = "evidences"
        });
        var vm = CreateVm(collection);

        Assert.Equal(2, vm.OrphanPassageCount);
        Assert.Equal(0, vm.OrphanConceptCount);
    }

    [Fact]
    public void RunForceDirectedLayout_PositionsNodes()
    {
        var collection = CreateTestCollection(passageCount: 3, conceptCount: 0);
        var vm = CreateVm(collection);

        vm.RunForceDirectedLayout(800, 600);

        // All nodes should have positions within bounds
        foreach (var node in vm.Nodes)
        {
            Assert.InRange(node.X, 30, 770);
            Assert.InRange(node.Y, 30, 570);
        }
    }

    [Fact]
    public void Constructor_BookAnnotationType_CreatesBookNode()
    {
        var collection = new ScholarCollection
        {
            Id = "book-test",
            Name = "Book Test",
            SchemaVersion = 2,
            Passages = new List<ScholarPassage>
            {
                new()
                {
                    Id = "book1",
                    ZhText = "Some book text",
                    Summary = "The Platform Sutra",
                    AnnotationType = "Book"
                },
                new()
                {
                    Id = "p0",
                    ZhText = "Regular passage"
                }
            }
        };

        var vm = CreateVm(collection);

        var bookNodes = vm.Nodes.Where(n => n.NodeType == ScholarNodeType.Book).ToList();
        Assert.Single(bookNodes);
        Assert.Equal("book1", bookNodes[0].NodeId);
        Assert.Equal("The Platform Sutra", bookNodes[0].Label);

        var passageNodes = vm.Nodes.Where(n => n.NodeType == ScholarNodeType.Passage).ToList();
        Assert.Single(passageNodes);
        Assert.Equal("p0", passageNodes[0].NodeId);
    }

    [Fact]
    public void Constructor_BookAnnotationType_CaseInsensitive()
    {
        var collection = new ScholarCollection
        {
            Id = "book-ci",
            Name = "Case Test",
            SchemaVersion = 2,
            Passages = new List<ScholarPassage>
            {
                new()
                {
                    Id = "book-lower",
                    ZhText = "Text",
                    AnnotationType = "book"
                }
            }
        };

        var vm = CreateVm(collection);

        var bookNodes = vm.Nodes.Where(n => n.NodeType == ScholarNodeType.Book).ToList();
        Assert.Single(bookNodes);
        Assert.Equal("book-lower", bookNodes[0].NodeId);
    }

    [Fact]
    public void Constructor_BookNode_HasBookColor()
    {
        var collection = new ScholarCollection
        {
            Id = "book-color",
            Name = "Color Test",
            SchemaVersion = 2,
            Passages = new List<ScholarPassage>
            {
                new()
                {
                    Id = "b1",
                    ZhText = "Text",
                    AnnotationType = "Book"
                }
            }
        };

        var vm = CreateVm(collection);

        var bookNode = vm.Nodes.First(n => n.NodeType == ScholarNodeType.Book);
        Assert.Equal("#D4A574", bookNode.ColorHex);
    }

    [Fact]
    public void GetVisibleNodes_ShowBooks_FiltersBookNodes()
    {
        var collection = new ScholarCollection
        {
            Id = "vis-book",
            Name = "Visibility Test",
            SchemaVersion = 2,
            Passages = new List<ScholarPassage>
            {
                new() { Id = "b1", ZhText = "Book", AnnotationType = "Book" },
                new() { Id = "p1", ZhText = "Passage" }
            }
        };

        var vm = CreateVm(collection);

        // By default ShowBooks is true
        var visible = vm.GetVisibleNodes();
        Assert.Contains(visible, n => n.NodeType == ScholarNodeType.Book);

        vm.ShowBooks = false;
        visible = vm.GetVisibleNodes();
        Assert.DoesNotContain(visible, n => n.NodeType == ScholarNodeType.Book);
        Assert.Contains(visible, n => n.NodeType == ScholarNodeType.Passage);
    }

    [Fact]
    public void ExecuteCommand_PushesToUndoStack()
    {
        var vm = CreateVm();
        Assert.False(vm.CanUndo);

        var concept = new ConceptNode { Id = "cmd-c", Name = "Via Command", Description = "" };
        vm.ExecuteCommand(new AddConceptCommand(vm, concept));

        Assert.True(vm.CanUndo);
        Assert.Contains(vm.Nodes, n => n.NodeId == "cmd-c");
    }

    [Fact]
    public void Undo_ReversesLastCommand()
    {
        var vm = CreateVm();
        var concept = new ConceptNode { Id = "undo-c", Name = "Undo Me", Description = "" };
        vm.ExecuteCommand(new AddConceptCommand(vm, concept));

        Assert.Contains(vm.Nodes, n => n.NodeId == "undo-c");

        vm.Undo();

        Assert.DoesNotContain(vm.Nodes, n => n.NodeId == "undo-c");
        Assert.True(vm.CanRedo);
    }

    [Fact]
    public void Redo_ReExecutesCommand()
    {
        var vm = CreateVm();
        var concept = new ConceptNode { Id = "redo-c", Name = "Redo Me", Description = "" };
        vm.ExecuteCommand(new AddConceptCommand(vm, concept));
        vm.Undo();

        Assert.DoesNotContain(vm.Nodes, n => n.NodeId == "redo-c");

        vm.Redo();

        Assert.Contains(vm.Nodes, n => n.NodeId == "redo-c");
        Assert.True(vm.CanUndo);
        Assert.False(vm.CanRedo);
    }

    [Fact]
    public void Undo_WhenEmpty_DoesNothing()
    {
        var vm = CreateVm();
        Assert.False(vm.CanUndo);

        // Should not throw
        vm.Undo();

        Assert.False(vm.CanUndo);
        Assert.False(vm.CanRedo);
    }
}

public class GraphCommandsTests
{
    private static ResearchGraphViewModel CreateVmWithPassages()
    {
        var collection = new ScholarCollection
        {
            Id = "cmd-test",
            Name = "Command Test",
            SchemaVersion = 2,
            Passages = new List<ScholarPassage>
            {
                new() { Id = "p0", ZhText = "Text 0" },
                new() { Id = "p1", ZhText = "Text 1" }
            }
        };
        return new ResearchGraphViewModel(collection, new List<ScholarCollection> { collection });
    }

    [Fact]
    public void AddConceptCommand_Execute_AddsNode()
    {
        var vm = CreateVmWithPassages();
        var concept = new ConceptNode { Id = "c-new", Name = "New", Description = "" };
        var cmd = new AddConceptCommand(vm, concept);

        cmd.Execute();

        Assert.Contains(vm.Nodes, n => n.NodeId == "c-new");
        Assert.Contains(vm.GetCollection().Concepts, c => c.Id == "c-new");
    }

    [Fact]
    public void AddConceptCommand_Undo_RemovesNode()
    {
        var vm = CreateVmWithPassages();
        var concept = new ConceptNode { Id = "c-rm", Name = "Remove Me", Description = "" };
        var cmd = new AddConceptCommand(vm, concept);
        cmd.Execute();

        cmd.Undo();

        Assert.DoesNotContain(vm.Nodes, n => n.NodeId == "c-rm");
        Assert.DoesNotContain(vm.GetCollection().Concepts, c => c.Id == "c-rm");
    }

    [Fact]
    public void AddEdgeCommand_Execute_CreatesEdge()
    {
        var vm = CreateVmWithPassages();
        var edge = new ScholarGraphEdge
        {
            Id = "e-new",
            FromNodeId = "p0",
            ToNodeId = "p1",
            RelationType = "quotes"
        };
        var cmd = new AddEdgeCommand(vm, edge);

        cmd.Execute();

        Assert.Single(vm.Edges);
        Assert.Equal("e-new", vm.Edges[0].EdgeId);
    }

    [Fact]
    public void AddEdgeCommand_Undo_RemovesEdgeAndDecrementsDegree()
    {
        var vm = CreateVmWithPassages();
        var edge = new ScholarGraphEdge
        {
            Id = "e-undo",
            FromNodeId = "p0",
            ToNodeId = "p1",
            RelationType = "quotes"
        };
        var cmd = new AddEdgeCommand(vm, edge);
        cmd.Execute();

        var fromNode = vm.Nodes.First(n => n.NodeId == "p0");
        var toNode = vm.Nodes.First(n => n.NodeId == "p1");
        Assert.Equal(1, fromNode.Degree);
        Assert.Equal(1, toNode.Degree);

        cmd.Undo();

        Assert.Empty(vm.Edges);
        Assert.Equal(0, fromNode.Degree);
        Assert.Equal(0, toNode.Degree);
    }

    [Fact]
    public void RemoveNodeCommand_Execute_RemovesNodeAndEdges()
    {
        var vm = CreateVmWithPassages();
        var concept = new ConceptNode { Id = "c-del", Name = "Delete Me", Description = "" };
        vm.AddConcept(concept);
        vm.AddEdge(new ScholarGraphEdge
        {
            Id = "e-del",
            FromNodeId = "p0",
            ToNodeId = "c-del",
            RelationType = "evidences"
        });

        var cmd = new RemoveNodeCommand(vm, "c-del");
        cmd.Execute();

        Assert.DoesNotContain(vm.Nodes, n => n.NodeId == "c-del");
        Assert.Empty(vm.Edges);
    }

    [Fact]
    public void RemoveNodeCommand_Undo_RestoresNodeAndEdges()
    {
        var vm = CreateVmWithPassages();
        var concept = new ConceptNode { Id = "c-restore", Name = "Restore Me", Description = "" };
        vm.AddConcept(concept);
        vm.AddEdge(new ScholarGraphEdge
        {
            Id = "e-restore",
            FromNodeId = "p0",
            ToNodeId = "c-restore",
            RelationType = "evidences"
        });

        var cmd = new RemoveNodeCommand(vm, "c-restore");
        cmd.Execute();

        Assert.DoesNotContain(vm.Nodes, n => n.NodeId == "c-restore");

        cmd.Undo();

        Assert.Contains(vm.Nodes, n => n.NodeId == "c-restore");
        Assert.Single(vm.Edges);
    }

    [Fact]
    public void RenameConceptCommand_Execute_ChangesName()
    {
        var vm = CreateVmWithPassages();
        var concept = new ConceptNode { Id = "c-rename", Name = "Old Name", Description = "" };
        vm.AddConcept(concept);

        var cmd = new RenameConceptCommand(vm, "c-rename", "Old Name", "New Name");
        cmd.Execute();

        Assert.Equal("New Name", concept.Name);
        var node = vm.Nodes.First(n => n.NodeId == "c-rename");
        Assert.Equal("New Name", node.Label);
    }

    [Fact]
    public void RenameConceptCommand_Undo_RestoresOldName()
    {
        var vm = CreateVmWithPassages();
        var concept = new ConceptNode { Id = "c-rename2", Name = "Original", Description = "" };
        vm.AddConcept(concept);

        var cmd = new RenameConceptCommand(vm, "c-rename2", "Original", "Changed");
        cmd.Execute();
        cmd.Undo();

        Assert.Equal("Original", concept.Name);
        var node = vm.Nodes.First(n => n.NodeId == "c-rename2");
        Assert.Equal("Original", node.Label);
    }
}

public class MigrationTests
{
    private static readonly MethodInfo MigrateToV2Method = typeof(ScholarCollectionsService)
        .GetMethod("MigrateToV2", BindingFlags.NonPublic | BindingFlags.Static)!;

    private static void CallMigrateToV2(ScholarCollection collection)
    {
        MigrateToV2Method.Invoke(null, new object[] { collection });
    }

    [Fact]
    public void MigrateToV2_ConvertsPassageLinksToEdges()
    {
        var collection = new ScholarCollection
        {
            Id = "mig-1",
            SchemaVersion = 1,
            Passages = new List<ScholarPassage>
            {
                new() { Id = "p0", ZhText = "A" },
                new() { Id = "p1", ZhText = "B" }
            },
            Links = new List<PassageLink>
            {
                new()
                {
                    Id = "link1",
                    FromPassageId = "p0",
                    ToPassageId = "p1",
                    RelationType = "quotes"
                }
            }
        };

        CallMigrateToV2(collection);

        Assert.Equal(2, collection.SchemaVersion);
        Assert.Single(collection.Edges);
        var edge = collection.Edges[0];
        Assert.Equal("link1", edge.Id);
        Assert.Equal("p0", edge.FromNodeId);
        Assert.Equal("p1", edge.ToNodeId);
        Assert.Equal("quotes", edge.RelationType);
        Assert.Equal(ScholarNodeType.Passage, edge.FromNodeType);
        Assert.Equal(ScholarNodeType.Passage, edge.ToNodeType);
    }

    [Fact]
    public void MigrateToV2_IsIdempotent()
    {
        var collection = new ScholarCollection
        {
            Id = "mig-idem",
            SchemaVersion = 1,
            Links = new List<PassageLink>
            {
                new()
                {
                    Id = "link1",
                    FromPassageId = "p0",
                    ToPassageId = "p1",
                    RelationType = "parallels"
                }
            }
        };

        CallMigrateToV2(collection);
        CallMigrateToV2(collection); // second call should be no-op

        Assert.Single(collection.Edges);
        Assert.Equal(2, collection.SchemaVersion);
    }

    [Fact]
    public void MigrateToV2_SkipsNullFromPassageId()
    {
        var collection = new ScholarCollection
        {
            Id = "mig-null-from",
            SchemaVersion = 1,
            Links = new List<PassageLink>
            {
                new()
                {
                    Id = "bad-link",
                    FromPassageId = "",
                    ToPassageId = "p1",
                    RelationType = "quotes"
                }
            }
        };

        CallMigrateToV2(collection);

        Assert.Empty(collection.Edges);
        Assert.Equal(2, collection.SchemaVersion);
    }

    [Fact]
    public void MigrateToV2_SkipsNullToPassageId()
    {
        var collection = new ScholarCollection
        {
            Id = "mig-null-to",
            SchemaVersion = 1,
            Links = new List<PassageLink>
            {
                new()
                {
                    Id = "bad-link",
                    FromPassageId = "p0",
                    ToPassageId = "",
                    RelationType = "quotes"
                }
            }
        };

        CallMigrateToV2(collection);

        Assert.Empty(collection.Edges);
        Assert.Equal(2, collection.SchemaVersion);
    }

    [Fact]
    public void MigrateToV2_AlreadyV2_NoOp()
    {
        var collection = new ScholarCollection
        {
            Id = "mig-v2",
            SchemaVersion = 2,
            Links = new List<PassageLink>
            {
                new()
                {
                    Id = "should-not-migrate",
                    FromPassageId = "p0",
                    ToPassageId = "p1",
                    RelationType = "quotes"
                }
            }
        };

        CallMigrateToV2(collection);

        Assert.Empty(collection.Edges);
        Assert.Equal(2, collection.SchemaVersion);
    }

    [Fact]
    public void MigrateToV2_GeneratesIdIfNull()
    {
        var collection = new ScholarCollection
        {
            Id = "mig-gen-id",
            SchemaVersion = 1,
            Links = new List<PassageLink>
            {
                new()
                {
                    Id = "", // empty ID should trigger generation
                    FromPassageId = "p0",
                    ToPassageId = "p1",
                    RelationType = "quotes"
                }
            }
        };

        CallMigrateToV2(collection);

        Assert.Single(collection.Edges);
        var edge = collection.Edges[0];
        Assert.False(string.IsNullOrEmpty(edge.Id));
        Assert.Equal(8, edge.Id.Length); // Guid.NewGuid().ToString("N")[..8]
    }
}

public class AutoEdgeTests
{
    private static ResearchGraphViewModel CreateVmWithMaster(
        int passageCount = 2,
        string masterName = "Linji",
        int masterPassageIndex = 0)
    {
        var collection = new ScholarCollection
        {
            Id = "auto-edge-test",
            Name = "Auto Edge Test",
            SchemaVersion = 2
        };

        for (int i = 0; i < passageCount; i++)
        {
            collection.Passages.Add(new ScholarPassage
            {
                Id = $"p{i}",
                ZhText = $"Chinese text {i}",
                EnText = $"English text {i}",
                MasterNames = i == masterPassageIndex
                    ? new List<string> { masterName }
                    : new List<string>()
            });
        }

        return new ResearchGraphViewModel(collection, new List<ScholarCollection> { collection });
    }

    [Fact]
    public void AutoEdges_MasterNodeGetsAttributedToEdge()
    {
        var vm = CreateVmWithMaster();

        var autoEdges = vm.Edges
            .Where(e => e.RelationType == "attributed-to")
            .ToList();

        Assert.Single(autoEdges);
        Assert.Equal("p0", autoEdges[0].From.NodeId);
        Assert.Equal("master:Linji", autoEdges[0].To.NodeId);
    }

    [Fact]
    public void AutoEdges_HaveIsAutoGeneratedTrue()
    {
        var vm = CreateVmWithMaster();

        var autoEdges = vm.Edges
            .Where(e => e.RelationType == "attributed-to")
            .ToList();

        Assert.Single(autoEdges);
        Assert.True(autoEdges[0].IsAutoGenerated);
    }

    [Fact]
    public void AutoEdges_MultiplePassagesSameMaster_CreatesMultipleEdges()
    {
        var collection = new ScholarCollection
        {
            Id = "multi-master",
            Name = "Multi Master",
            SchemaVersion = 2,
            Passages = new List<ScholarPassage>
            {
                new() { Id = "p0", ZhText = "A", MasterNames = new List<string> { "Linji" } },
                new() { Id = "p1", ZhText = "B", MasterNames = new List<string> { "Linji" } },
                new() { Id = "p2", ZhText = "C", MasterNames = new List<string>() }
            }
        };

        var vm = new ResearchGraphViewModel(collection, new List<ScholarCollection> { collection });

        var autoEdges = vm.Edges.Where(e => e.IsAutoGenerated).ToList();
        Assert.Equal(2, autoEdges.Count);
        Assert.All(autoEdges, e => Assert.Equal("master:Linji", e.To.NodeId));
        Assert.All(autoEdges, e => Assert.True(e.IsAutoGenerated));
    }

    [Fact]
    public void AutoEdges_IncrementDegreeOnBothNodes()
    {
        var vm = CreateVmWithMaster();

        var passageNode = vm.Nodes.First(n => n.NodeId == "p0");
        var masterNode = vm.Nodes.First(n => n.NodeId == "master:Linji");

        Assert.Equal(1, passageNode.Degree);
        Assert.Equal(1, masterNode.Degree);
    }

    [Fact]
    public void AutoEdges_EdgeIdStartsWithAutoPrefix()
    {
        var vm = CreateVmWithMaster();

        var autoEdge = vm.Edges.First(e => e.IsAutoGenerated);
        Assert.StartsWith("auto:attributed:", autoEdge.EdgeId);
    }

    [Fact]
    public void AutoEdges_BookPassages_DoNotGetAutoEdges()
    {
        var collection = new ScholarCollection
        {
            Id = "book-no-auto",
            Name = "Book No Auto",
            SchemaVersion = 2,
            Passages = new List<ScholarPassage>
            {
                new()
                {
                    Id = "b1",
                    ZhText = "Book text",
                    AnnotationType = "Book",
                    MasterNames = new List<string> { "Linji" }
                }
            }
        };

        var vm = new ResearchGraphViewModel(collection, new List<ScholarCollection> { collection });

        var autoEdges = vm.Edges.Where(e => e.IsAutoGenerated).ToList();
        Assert.Empty(autoEdges);
    }

    [Fact]
    public void RemoveEdge_AutoGenerated_SkipsCollectionEdges()
    {
        var collection = new ScholarCollection
        {
            Id = "remove-auto",
            Name = "Remove Auto",
            SchemaVersion = 2,
            Passages = new List<ScholarPassage>
            {
                new() { Id = "p0", ZhText = "A", MasterNames = new List<string> { "Linji" } },
                new() { Id = "p1", ZhText = "B" }
            }
        };
        collection.Edges.Add(new ScholarGraphEdge
        {
            Id = "manual-e1",
            FromNodeId = "p0",
            ToNodeId = "p1",
            RelationType = "quotes"
        });

        var vm = new ResearchGraphViewModel(collection, new List<ScholarCollection> { collection });

        var autoEdge = vm.Edges.First(e => e.IsAutoGenerated);
        Assert.Single(collection.Edges);

        vm.RemoveEdge(autoEdge.EdgeId);

        Assert.DoesNotContain(vm.Edges, e => e.EdgeId == autoEdge.EdgeId);
        Assert.Single(collection.Edges);
        Assert.Equal("manual-e1", collection.Edges[0].Id);
    }

    [Fact]
    public void RemoveEdge_ManualEdge_RemovesFromCollectionEdges()
    {
        var collection = new ScholarCollection
        {
            Id = "remove-manual",
            Name = "Remove Manual",
            SchemaVersion = 2,
            Passages = new List<ScholarPassage>
            {
                new() { Id = "p0", ZhText = "A" },
                new() { Id = "p1", ZhText = "B" }
            }
        };
        collection.Edges.Add(new ScholarGraphEdge
        {
            Id = "manual-e1",
            FromNodeId = "p0",
            ToNodeId = "p1",
            RelationType = "quotes"
        });

        var vm = new ResearchGraphViewModel(collection, new List<ScholarCollection> { collection });

        vm.RemoveEdge("manual-e1");

        Assert.Empty(vm.Edges);
        Assert.Empty(collection.Edges);
    }
}

public class SuppressionTests
{
    [Fact]
    public void RemoveNode_MasterNode_AddsSuppressedAutoNodeId()
    {
        var collection = new ScholarCollection
        {
            Id = "suppress-test",
            Name = "Suppress Test",
            SchemaVersion = 2,
            Passages = new List<ScholarPassage>
            {
                new() { Id = "p0", ZhText = "A", MasterNames = new List<string> { "Linji" } },
                new() { Id = "p1", ZhText = "B" }
            }
        };

        var vm = new ResearchGraphViewModel(collection, new List<ScholarCollection> { collection });
        Assert.Contains(vm.Nodes, n => n.NodeId == "master:Linji");

        vm.RemoveNode("master:Linji");

        Assert.Contains("master:Linji", collection.SuppressedAutoNodeIds);
    }

    [Fact]
    public void SuppressedMaster_DoesNotReappearAfterRebuildGraph()
    {
        var collection = new ScholarCollection
        {
            Id = "suppress-rebuild",
            Name = "Suppress Rebuild",
            SchemaVersion = 2,
            Passages = new List<ScholarPassage>
            {
                new() { Id = "p0", ZhText = "A", MasterNames = new List<string> { "Linji" } }
            }
        };

        var vm = new ResearchGraphViewModel(collection, new List<ScholarCollection> { collection });
        Assert.Contains(vm.Nodes, n => n.NodeId == "master:Linji");

        vm.RemoveNode("master:Linji");
        Assert.DoesNotContain(vm.Nodes, n => n.NodeId == "master:Linji");

        vm.RebuildGraph();

        Assert.DoesNotContain(vm.Nodes, n => n.NodeId == "master:Linji");
        Assert.DoesNotContain(vm.Edges, e => e.To.NodeId == "master:Linji");
    }

    [Fact]
    public void SuppressedMaster_AutoEdgesAlsoSuppressed()
    {
        var collection = new ScholarCollection
        {
            Id = "suppress-edges",
            Name = "Suppress Edges",
            SchemaVersion = 2,
            Passages = new List<ScholarPassage>
            {
                new() { Id = "p0", ZhText = "A", MasterNames = new List<string> { "Linji" } },
                new() { Id = "p1", ZhText = "B", MasterNames = new List<string> { "Linji" } }
            }
        };

        var vm = new ResearchGraphViewModel(collection, new List<ScholarCollection> { collection });
        Assert.Equal(2, vm.Edges.Count(e => e.IsAutoGenerated));

        vm.RemoveNode("master:Linji");

        Assert.Empty(vm.Edges.Where(e => e.IsAutoGenerated));
    }

    [Fact]
    public void RestoreSuppressedNodes_ClearsListAndRebuilds()
    {
        var collection = new ScholarCollection
        {
            Id = "restore-test",
            Name = "Restore Test",
            SchemaVersion = 2,
            Passages = new List<ScholarPassage>
            {
                new() { Id = "p0", ZhText = "A", MasterNames = new List<string> { "Linji" } }
            }
        };

        var vm = new ResearchGraphViewModel(collection, new List<ScholarCollection> { collection });

        vm.RemoveNode("master:Linji");
        Assert.Single(collection.SuppressedAutoNodeIds);
        Assert.DoesNotContain(vm.Nodes, n => n.NodeId == "master:Linji");

        vm.RestoreSuppressedNodes();

        Assert.Empty(collection.SuppressedAutoNodeIds);
        Assert.Contains(vm.Nodes, n => n.NodeId == "master:Linji");
        Assert.Contains(vm.Edges, e => e.IsAutoGenerated && e.To.NodeId == "master:Linji");
    }

    [Fact]
    public void SuppressedAutoNodeIds_IsPersistedOnScholarCollection()
    {
        var collection = new ScholarCollection
        {
            Id = "persist-test",
            Name = "Persist Test",
            SchemaVersion = 2,
            Passages = new List<ScholarPassage>
            {
                new() { Id = "p0", ZhText = "A", MasterNames = new List<string> { "Linji" } }
            }
        };

        var vm = new ResearchGraphViewModel(collection, new List<ScholarCollection> { collection });

        vm.RemoveNode("master:Linji");

        Assert.Contains("master:Linji", collection.SuppressedAutoNodeIds);
        Assert.IsType<HashSet<string>>(collection.SuppressedAutoNodeIds);

        // Simulate creating a new VM from the same (persisted) collection
        var vm2 = new ResearchGraphViewModel(collection, new List<ScholarCollection> { collection });

        Assert.DoesNotContain(vm2.Nodes, n => n.NodeId == "master:Linji");
        Assert.Empty(vm2.Edges.Where(e => e.To.NodeId == "master:Linji"));
    }

    [Fact]
    public void RemoveNode_NonAutoNode_DoesNotAddToSuppressed()
    {
        var collection = new ScholarCollection
        {
            Id = "no-suppress",
            Name = "No Suppress",
            SchemaVersion = 2,
            Passages = new List<ScholarPassage>
            {
                new() { Id = "p0", ZhText = "A" }
            },
            Concepts = new List<ConceptNode>
            {
                new() { Id = "c0", Name = "Test Concept", Description = "desc" }
            }
        };

        var vm = new ResearchGraphViewModel(collection, new List<ScholarCollection> { collection });

        vm.RemoveNode("c0");

        Assert.Empty(collection.SuppressedAutoNodeIds);
    }

    [Fact]
    public void RemoveNode_MasterNode_RemovesAutoEdgesWithoutTouchingCollectionEdges()
    {
        var collection = new ScholarCollection
        {
            Id = "master-remove-edges",
            Name = "Master Remove Edges",
            SchemaVersion = 2,
            Passages = new List<ScholarPassage>
            {
                new() { Id = "p0", ZhText = "A", MasterNames = new List<string> { "Linji" } },
                new() { Id = "p1", ZhText = "B" }
            }
        };
        collection.Edges.Add(new ScholarGraphEdge
        {
            Id = "manual-e1",
            FromNodeId = "p0",
            ToNodeId = "p1",
            RelationType = "quotes"
        });

        var vm = new ResearchGraphViewModel(collection, new List<ScholarCollection> { collection });

        Assert.Equal(2, vm.Edges.Count);
        Assert.Single(collection.Edges);

        vm.RemoveNode("master:Linji");

        Assert.Single(vm.Edges);
        Assert.Equal("manual-e1", vm.Edges[0].EdgeId);
        Assert.Single(collection.Edges);
    }
}
