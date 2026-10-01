namespace OrderTracking.Application.Constants;

/// <summary>Общие сообщения об ошибках при работе с заказами.</summary>
public static class OrderErrorMessages
{
    /// <summary>Конфликт версии при одновременном изменении заказа.</summary>
    public const string VersionConflict = "Заказ уже изменён. Обновите данные и повторите действие.";

    /// <summary>Попытка создать заказ с уже занятым номером.</summary>
    public const string DuplicateNumber = "Заказ с таким номером уже существует.";
}
