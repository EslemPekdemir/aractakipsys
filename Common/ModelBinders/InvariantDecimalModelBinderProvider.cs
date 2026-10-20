// File: Common/ModelBinders/InvariantDecimalModelBinderProvider.cs
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace aractakipsys.Common.ModelBinders;

/// <summary>
/// decimal ve decimal? türündeki tüm modeller için InvariantDecimalModelBinder'ı devreye alan sağlayıcı.
/// </summary>
public class InvariantDecimalModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (context.Metadata.ModelType == typeof(decimal) || context.Metadata.ModelType == typeof(decimal?))
        {
            return new InvariantDecimalModelBinder();
        }

        return null;
    }
}
