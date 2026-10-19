using Microsoft.AspNetCore.Mvc;

namespace EnterpriseManagement.Api.Common;

// Phân trang tuỳ chọn cho các API trả danh sách có thể dài. Không gửi page/pageSize thì trả đủ như cũ (app/web cũ không hỏng);
// có gửi thì trả đúng một trang và tổng số dòng nằm ở header X-Total-Count để client biết còn nữa hay không.
public static class Paging
{
    public const int DefaultPageSize = 30;
    public const int MaxPageSize = 200;
    public const string TotalHeader = "X-Total-Count";

    public static IEnumerable<T> Paged<T>(this ControllerBase controller, IEnumerable<T> source, int? page, int? pageSize)
    {
        if (page is null && pageSize is null) return source;

        var list = source as IReadOnlyList<T> ?? source.ToList();
        var size = Math.Clamp(pageSize ?? DefaultPageSize, 1, MaxPageSize);
        var number = Math.Max(page ?? 1, 1);

        controller.Response.Headers[TotalHeader] = list.Count.ToString();
        controller.Response.Headers["Access-Control-Expose-Headers"] = TotalHeader;
        return list.Skip((number - 1) * size).Take(size).ToList();
    }

    // Khớp không phân biệt hoa thường và không phân biệt dấu cách thừa; rỗng nghĩa là không lọc.
    public static bool Matches(string? query, params string?[] fields)
    {
        var q = query?.Trim();
        if (string.IsNullOrEmpty(q)) return true;
        return fields.Any(f => f is not null && f.Contains(q, StringComparison.OrdinalIgnoreCase));
    }
}
