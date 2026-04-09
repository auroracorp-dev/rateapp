using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using WebAPI.Controllers;
using WebAPI.Models;

public static class Utils
{
    public static string GenerateInviteCode(int userId)
    {
        const int totalLength = 20;
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789-_";

        // userId 기반 Base64 문자열
        string base64Id = Convert.ToBase64String(BitConverter.GetBytes(userId)).TrimEnd('='); // '=' 제거

        // GUID 기반 랜덤 문자열 생성
        // GUID = 8(시간) 4(시간) 4(버전) 4(키) 12(랜덤)의 원리로 랜덤 난수
        string guidPart = Guid.NewGuid().ToString("N"); // 32자리 16진수 문자열

        char[] code = new char[totalLength];
        int i = 0;

        // Base64 ID 먼저 넣기
        for (; i < base64Id.Length && i < totalLength; i++)
            code[i] = base64Id[i];

        // 나머지는 랜덤 GUID로 채우기
        for (int j = 0; i < totalLength && j < guidPart.Length; i++, j++)
            code[i] = chars[guidPart[j] % chars.Length];

        return new string(code);
    }

    public static bool HasManagerPermission(Role user)
    {
        return user >= Role.Manager;
    }

    public static bool HasSuperUserPermission(Role user)
    {
        return user >= Role.SuperUser;
    }

    public class RequireUserAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (AuthController.CurrentUser == null)
            {
                context.Result = new UnauthorizedResult();
            }
        }
    }

    public static User EnsureUser(User? user)
    {
        if (user != null) return user;

        var currentUser = AuthController.CurrentUser;
        if (currentUser != null) return currentUser;

        throw new UnauthorizedAccessException("로그인 필요");
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; }
        public int TotalCount { get; set; }
    }

    public const int pageCount = 10;
}

public struct Unit
{
}

public static class TextFilter
{
    // 금지 단어 리스트
    private static readonly string[] BadWords = new[]
    {
        "욕설1", "욕설2", "나쁜말" // 실제로는 여기서 더 많이 추가
    };
    public static bool ContainsBadWord(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return true;

        foreach (var word in BadWords)
        {
            // 대소문자 무시, 단순 포함 여부
            if (Regex.IsMatch(input, Regex.Escape(word), RegexOptions.IgnoreCase))
                return true;
        }

        return false;
    }

    public static bool CompareText(string input1, string input2)
        { return input1.ToLower() == input2.ToLower(); }
}

public static class PagingHelper
{
    public static async Task<List<Tout>> GetPageData<T, Tout, TKey>( IQueryable<T> query, int page,
        Expression<Func<T, TKey>> orderBy, Expression<Func<T, Tout>> selector, bool descending = true)
    {
        if (descending)
            query = query.OrderByDescending(orderBy);
        else
            query = query.OrderBy(orderBy);

        return await query
            .Skip((page - 1) * Utils.pageCount)
            .Take(Utils.pageCount)
            .Select(selector)
            .ToListAsync();
    }
}