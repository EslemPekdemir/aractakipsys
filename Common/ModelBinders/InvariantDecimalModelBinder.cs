// File: Common/ModelBinders/InvariantDecimalModelBinder.cs
using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace aractakipsys.Common.ModelBinders;

/// <summary>
/// Hem nokta (.) hem virgül (,) ondalık basamak ayracını akıllıca tanıyan,
/// HTML5 number input'larından gelen "5.0" gibi değerlerin Türkçe kültürde "50" olmasını önleyen Model Binder.
/// </summary>
public class InvariantDecimalModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext == null)
        {
            throw new ArgumentNullException(nameof(bindingContext));
        }

        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueProviderResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

        var rawValue = valueProviderResult.FirstValue;
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            // Nullable decimal (decimal?) ise null olarak bağla
            if (bindingContext.ModelType == typeof(decimal?))
            {
                bindingContext.Result = ModelBindingResult.Success(null);
            }
            return Task.CompletedTask;
        }

        var cleanValue = rawValue.Trim();

        decimal parsedValue;
        bool success;

        // Hem nokta hem virgül içeriyorsa (Örn: "1.250,50" veya "1,250.50")
        if (cleanValue.Contains('.') && cleanValue.Contains(','))
        {
            int lastDot = cleanValue.LastIndexOf('.');
            int lastComma = cleanValue.LastIndexOf(',');

            if (lastComma > lastDot)
            {
                // Türk formatı: "1.250,50" -> noktayı (binlik) kaldır, virgülü noktaya çevir
                var normalized = cleanValue.Replace(".", "").Replace(',', '.');
                success = decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out parsedValue);
            }
            else
            {
                // İngiliz formatı: "1,250.50" -> virgülü (binlik) kaldır
                var normalized = cleanValue.Replace(",", "");
                success = decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out parsedValue);
            }
        }
        else
        {
            // Yalnızca nokta veya yalnızca virgül içeriyorsa (Örn: "5.0", "5,0", "5.5", "5,5")
            // Virgülü noktaya çevirerek InvariantCulture ile çözümle
            var normalized = cleanValue.Replace(',', '.');
            success = decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out parsedValue);
        }

        if (success)
        {
            bindingContext.Result = ModelBindingResult.Success(parsedValue);
        }
        else
        {
            bindingContext.ModelState.TryAddModelError(
                bindingContext.ModelName,
                $"{bindingContext.ModelMetadata.DisplayName ?? bindingContext.ModelName} geçerli bir sayı olmalıdır.");
        }

        return Task.CompletedTask;
    }
}
