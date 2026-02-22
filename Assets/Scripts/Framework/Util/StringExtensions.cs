using UnityEngine;

public static class StringExtensions 
{
    public static string ToTitleCase(this string str)
    {
        if (str.Length > 0)
        {
            return char.ToUpper(str[0]) + str.Substring(1);
        }
        return str;
    }
    public static string ConvertToKMB(this int num)
    {
        if (num >= 1000000000)
        {
            return (num / 1000000000).ToString("0.##") + "B";
        }
        if (num >= 1000000)
        {
            return (num / 1000000).ToString("0.##") + "M";
        }
        if (num >= 1000)
        {
            return (num / 1000).ToString("0.##") + "K";
        }
        return num.ToString();
    }
}
