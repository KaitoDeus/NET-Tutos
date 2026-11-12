using System.Text;

namespace NET_Tutos.BuildingBlocks.Common.Sandbox;

/// <summary>
/// Bounded UTF-16 <see cref="TextWriter"/> that caps captured standard output and error text
/// to protect sandbox worker processes from unbounded memory allocation.
/// </summary>
public sealed class BoundedStringWriter : TextWriter
{
    private readonly int _maxCharacters;
    private readonly StringBuilder _buffer;
    private bool _truncatedNoticeAppended;

    public BoundedStringWriter(int maxCharacters = SandboxGuardrails.MaxOutputCharacters)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxCharacters);
        _maxCharacters = maxCharacters;
        _buffer = new StringBuilder(Math.Min(maxCharacters, 1024));
    }

    public bool IsTruncated { get; private set; }

    public override Encoding Encoding => Encoding.Unicode;

    public override void Write(char value)
    {
        if (_buffer.Length < _maxCharacters)
        {
            _buffer.Append(value);
            return;
        }

        MarkTruncated();
    }

    public override void Write(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        AppendBounded(value.AsSpan());
    }

    public override void Write(char[] buffer, int index, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        if (index < 0 || count < 0 || index + count > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        if (count == 0)
        {
            return;
        }

        AppendBounded(buffer.AsSpan(index, count));
    }

    private void AppendBounded(ReadOnlySpan<char> span)
    {
        int remaining = _maxCharacters - _buffer.Length;
        if (remaining > 0)
        {
            int toCopy = Math.Min(remaining, span.Length);
            _buffer.Append(span[..toCopy]);
            if (toCopy == span.Length)
            {
                return;
            }
        }

        MarkTruncated();
    }

    private void MarkTruncated()
    {
        IsTruncated = true;
        if (_truncatedNoticeAppended)
        {
            return;
        }

        _truncatedNoticeAppended = true;
        _buffer.Append($"\n[... Output truncated at {_maxCharacters} characters for sandbox memory protection ...]");
    }

    public override string ToString() => _buffer.ToString();
}
