using System.Text;
using Xunit;

namespace FileTest;

public class UnitTest1
{
    [Fact]
    public void Test1()
    {
        StringBuilder builder = new();
        new EmbedTemplate().TransformAppend(builder, 4);
        var x = builder.ToString();
        Assert.StartsWith("    ", x);
    }
}