// File: Models/ViewModels/AjaxResultViewModel.cs
namespace aractakipsys.Models.ViewModels;

public class AjaxResultViewModel
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }

    public static AjaxResultViewModel Ok(string message = "İşlem başarılı.", object? data = null)
    {
        return new AjaxResultViewModel
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    public static AjaxResultViewModel Fail(string message = "Bir hata oluştu.", object? data = null)
    {
        return new AjaxResultViewModel
        {
            Success = false,
            Message = message,
            Data = data
        };
    }

    public static AjaxResultViewModel Error(string message = "Bir hata oluştu.", object? data = null)
    {
        return Fail(message, data);
    }
}
