using VSProjectRenamer.Core;

namespace VSProjectRenamer.Tests;

public class NamingConventionGeneratorTests
{
    // -----------------------------------------------------------------------
    // Word splitting
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData("MyProjectName",   new[] { "My", "Project", "Name" })]
    [InlineData("myProjectName",   new[] { "my", "Project", "Name" })]
    [InlineData("ABPFramework",    new[] { "ABP", "Framework" })]
    [InlineData("my-project-name", new[] { "my", "project", "name" })]
    [InlineData("my_project_name", new[] { "my", "project", "name" })]
    [InlineData("MY_PROJECT_NAME", new[] { "MY", "PROJECT", "NAME" })]
    [InlineData("my.project.name", new[] { "my", "project", "name" })]
    [InlineData("BookStore",       new[] { "Book", "Store" })]
    [InlineData("SimpleWord",      new[] { "Simple", "Word" })]
    public void SplitIntoWords_ShouldReturnExpectedWords(string input, string[] expected)
    {
        var words = NamingConventionGenerator.SplitIntoWords(input);
        Assert.Equal(expected, words);
    }

    // -----------------------------------------------------------------------
    // Full variant generation
    // -----------------------------------------------------------------------

    [Fact]
    public void Generate_MyProjectName_ReturnsAll12Variants()
    {
        var v = NamingConventionGenerator.Generate("MyProjectName");

        Assert.Equal("MyProjectName",    v.PascalCase);
        Assert.Equal("myProjectName",    v.CamelCase);
        Assert.Equal("my_project_name",  v.SnakeCase);
        Assert.Equal("my-project-name",  v.KebabCase);
        Assert.Equal("MY_PROJECT_NAME",  v.UpperSnakeCase);
        Assert.Equal("my.project.name",  v.DotCase);
        Assert.Equal("My Project Name",  v.TitleCase);
        Assert.Equal("MYPROJECTNAME",    v.UpperFlatCase);
        Assert.Equal("myprojectname",    v.LowerFlatCase);
        Assert.Equal("MPN",              v.Acronym);
        Assert.Equal("my project name",  v.LowerSpaceCase);
        Assert.Equal("MY PROJECT NAME",  v.UpperSpaceCase);
    }

    [Fact]
    public void Generate_BookStore_ReturnsAll12Variants()
    {
        var v = NamingConventionGenerator.Generate("BookStore");

        Assert.Equal("BookStore",    v.PascalCase);
        Assert.Equal("bookStore",    v.CamelCase);
        Assert.Equal("book_store",   v.SnakeCase);
        Assert.Equal("book-store",   v.KebabCase);
        Assert.Equal("BOOK_STORE",   v.UpperSnakeCase);
        Assert.Equal("book.store",   v.DotCase);
        Assert.Equal("Book Store",   v.TitleCase);
        Assert.Equal("BOOKSTORE",    v.UpperFlatCase);
        Assert.Equal("bookstore",    v.LowerFlatCase);
        Assert.Equal("BS",           v.Acronym);
        Assert.Equal("book store",   v.LowerSpaceCase);
        Assert.Equal("BOOK STORE",   v.UpperSpaceCase);
    }

    [Fact]
    public void Generate_ABPFramework_HandlesAllCapsWord()
    {
        var v = NamingConventionGenerator.Generate("ABPFramework");

        Assert.Equal("ABPFramework",   v.PascalCase);
        Assert.Equal("aBPFramework",   v.CamelCase);
        Assert.Equal("abp_framework",  v.SnakeCase);
        Assert.Equal("abp-framework",  v.KebabCase);
        Assert.Equal("ABP_FRAMEWORK",  v.UpperSnakeCase);
        Assert.Equal("abp.framework",  v.DotCase);
        Assert.Equal("ABP Framework",  v.TitleCase);
        Assert.Equal("ABPFRAMEWORK",   v.UpperFlatCase);
        Assert.Equal("abpframework",   v.LowerFlatCase);
        Assert.Equal("AF",             v.Acronym);
        Assert.Equal("abp framework",  v.LowerSpaceCase);
        Assert.Equal("ABP FRAMEWORK",  v.UpperSpaceCase);
    }

    [Fact]
    public void Generate_FromKebabCase_ProducesSameResultAsPascalCase()
    {
        var fromPascal = NamingConventionGenerator.Generate("BookStore");
        var fromKebab  = NamingConventionGenerator.Generate("book-store");

        Assert.Equal(fromPascal.PascalCase,    fromKebab.PascalCase);
        Assert.Equal(fromPascal.SnakeCase,     fromKebab.SnakeCase);
        Assert.Equal(fromPascal.KebabCase,     fromKebab.KebabCase);
        Assert.Equal(fromPascal.UpperFlatCase, fromKebab.UpperFlatCase);
        Assert.Equal(fromPascal.LowerFlatCase, fromKebab.LowerFlatCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Generate_EmptyOrWhitespace_ThrowsArgumentException(string input)
    {
        Assert.Throws<ArgumentException>(() => NamingConventionGenerator.Generate(input));
    }

    [Fact]
    public void Generate_SingleWord_HandlesGracefully()
    {
        var v = NamingConventionGenerator.Generate("Shop");

        Assert.Equal("Shop",   v.PascalCase);
        Assert.Equal("shop",   v.CamelCase);
        Assert.Equal("shop",   v.SnakeCase);
        Assert.Equal("shop",   v.KebabCase);
        Assert.Equal("SHOP",   v.UpperSnakeCase);
        Assert.Equal("shop",   v.DotCase);
        Assert.Equal("Shop",   v.TitleCase);
        Assert.Equal("SHOP",   v.UpperFlatCase);
        Assert.Equal("shop",   v.LowerFlatCase);
        Assert.Equal("S",      v.Acronym);
        Assert.Equal("shop",   v.LowerSpaceCase);
        Assert.Equal("SHOP",   v.UpperSpaceCase);
    }

    // -----------------------------------------------------------------------
    // All() enumeration
    // -----------------------------------------------------------------------

    [Fact]
    public void All_ReturnsExactly12Elements()
    {
        var v = NamingConventionGenerator.Generate("MyProject");
        Assert.Equal(12, v.All().Count());
    }
}
