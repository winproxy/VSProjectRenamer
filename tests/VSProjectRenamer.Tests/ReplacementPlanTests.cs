using VSProjectRenamer.Core;

namespace VSProjectRenamer.Tests;

public class ReplacementPlanTests
{
    [Fact]
    public void Apply_ReplacesAllVariantsInText()
    {
        var oldV = NamingConventionGenerator.Generate("BookStore");
        var newV = NamingConventionGenerator.Generate("ProductCatalog");
        var plan = new ReplacementPlan(oldV, newV);

        var input  = "BookStore bookStore book_store book-store BOOK_STORE bookstore BOOKSTORE BS Book Store";
        var result = plan.Apply(input);

        Assert.Contains("ProductCatalog",    result);
        Assert.Contains("productCatalog",    result);
        Assert.Contains("product_catalog",   result);
        Assert.Contains("product-catalog",   result);
        Assert.Contains("PRODUCT_CATALOG",   result);
        Assert.Contains("productcatalog",    result);
        Assert.Contains("PRODUCTCATALOG",    result);
        Assert.Contains("PC",                result);
        Assert.Contains("Product Catalog",   result);
        Assert.DoesNotContain("BookStore",   result);
        Assert.DoesNotContain("book_store",  result);
    }

    [Fact]
    public void Apply_LongestMatchFirst_NoPartialReplacement()
    {
        // Ensure BOOK_STORE is replaced before BOOK or STORE
        var oldV = NamingConventionGenerator.Generate("BookStore");
        var newV = NamingConventionGenerator.Generate("ProductCatalog");
        var plan = new ReplacementPlan(oldV, newV);

        var result = plan.Apply("BOOK_STORE");
        Assert.Equal("PRODUCT_CATALOG", result);
    }

    [Fact]
    public void Pairs_ExcludesUnchangedVariants()
    {
        var oldV = NamingConventionGenerator.Generate("Shop");
        var newV = NamingConventionGenerator.Generate("Shop");  // same name
        var plan = new ReplacementPlan(oldV, newV);

        Assert.Empty(plan.Pairs);
    }
}
