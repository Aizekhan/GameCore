// Assets/Scripts/UI/Attributes/UIPanelAutoRegisterAttribute.cs
using System;

namespace GameCore.Core
{
    /// <summary>
    /// Атрибут для автоматичної реєстрації UI панелей.
    /// Додавайте цей атрибут до класів UI панелей для автоматичної реєстрації
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class UIPanelAutoRegisterAttribute : Attribute
    {
        public string CustomName { get; private set; }
        public string Category { get; private set; }

        /// <summary>
        /// Сконструювати атрибут із необов'язковими ім'ям та категорією для реєстрації
        /// </summary>
        /// <param name="customName">Необов'язкове ім'я панелі (якщо не вказано, використовується ім'я ґейм-об'єкта)</param>
        /// <param name="category">Необов'язкова категорія для групування панелей</param>
        public UIPanelAutoRegisterAttribute(string customName = null, string category = "Default")
        {
            CustomName = customName;
            Category = category;
        }
    }
}