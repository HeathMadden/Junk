using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace PureIP.Portal.Customer.Controllers
{
    public static class Helper
    {
        public static List<SelectListItem> ToSelectList(this List<string> list, bool addSelect = true)
        {
            var result = list.Select(i => new SelectListItem() { Value = i, Text = i }).ToList();
            if (addSelect) result.AddSelect();
            return result;
        }
        public static List<SelectListItem> ToSelectList<T>(this List<T> list, Func<T, string> value, Func<T, string> text, bool addSelect = true)
        {
            var result = list.Select(i => new SelectListItem() { Value = value.Invoke(i), Text = text.Invoke(i) }).ToList();
            if (addSelect) result.AddSelect();
            return result;
        }

        public static List<SelectListItem> ToSelectList<T>(bool addSelect = true, bool displayNameOnly = false, bool nameAsValue = false) where T : Enum
        {
            var result = Enum.GetValues(typeof(T)).Cast<T>().ToList().ToSelectList(
                e => nameAsValue ? displayNameOnly ? $"{e.GetDisplayName()}" : $"{e.ToString()} ({e.GetDisplayName()})" : Convert.ToInt32(e).ToString(),
                e => displayNameOnly ? $"{e.GetDisplayName()}" : $"{e.ToString()} ({e.GetDisplayName()})", addSelect);
            return result;
        }


        public static List<SelectListItem> AddSelect(this List<SelectListItem> list)
        {
            list.Insert(0, new SelectListItem() { Text = "" });
            return list;
        }

        public static string GetDisplayName<T>(Expression<Func<T, object>> propertyExpression)
        {
            var memberInfo = GetPropertyInformation(propertyExpression.Body);
            if (memberInfo == null) { throw new ArgumentException("No property reference expression was found.", "propertyExpression"); }

            var attr = memberInfo.GetCustomAttribute<DisplayNameAttribute>();
            if (attr == null) { return memberInfo.Name; }

            return attr.DisplayName;
        }

        public static MemberInfo GetPropertyInformation(Expression propertyExpression)
        {
            MemberExpression memberExpr = propertyExpression as MemberExpression;
            if (memberExpr == null)
            {
                UnaryExpression unaryExpr = propertyExpression as UnaryExpression;
                if (unaryExpr != null && unaryExpr.NodeType == ExpressionType.Convert)
                {
                    memberExpr = unaryExpr.Operand as MemberExpression;
                }
            }

            if (memberExpr != null && memberExpr.Member.MemberType == MemberTypes.Property)
            {
                return memberExpr.Member;
            }

            return null;
        }
    }
}
