using System.Text;
using Xunit;

namespace FileTest;

[RuntimeT4Generator.T4(isIndent: true)]
partial struct EmbedTemplate
{
}

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
