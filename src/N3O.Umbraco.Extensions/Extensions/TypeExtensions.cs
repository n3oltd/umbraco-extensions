using Humanizer;
using N3O.Umbraco.Attributes;
using N3O.Umbraco.Lookups;
using N3O.Umbraco.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core;

namespace N3O.Umbraco.Extensions;

public static class ReflectionExtensions {
    public static IReadOnlyList<Type> ApplyAttributeOrdering(this IEnumerable<Type> source) {
        var orderedList = source.OrderBy(x => x.HasAttribute<OrderAttribute>() ? 0 : 1)
                                .ThenBy(x => x.GetCustomAttribute<OrderAttribute>()?.Order)
                                .ToList();

        return orderedList;
    }

    public static IReadOnlyList<T> ApplyAttributeOrdering<T>(this IEnumerable<T> source) {
        var orderedList = source.OrderBy(x => x.GetType().HasAttribute<OrderAttribute>() ? 0 : 1)
                                .ThenBy(x => x.GetType().GetCustomAttribute<OrderAttribute>()?.Order)
                                .ToList();

        return orderedList;
    }

    public static MethodCallBuilder CallMethod(this object target, string name) {
        return new MethodCallBuilder(target.GetType(), target, name);
    }

    public static MethodCallBuilder CallStaticMethod(this Type staticType, string name) {
        return new MethodCallBuilder(staticType, null, name);
    }

    public static TType CreateInstance<TType, TParameter1>(this Type type,
                                                           TParameter1 parameter1) {
        return CreateInstance<TType>(type, parameter1);
    }

    public static TType CreateInstance<TType, TParameter1, TParameter2>(this Type type,
                                                                        TParameter1 parameter1,
                                                                        TParameter2 parameter2) {
        return CreateInstance<TType>(type, parameter1, parameter2);
    }

    public static TType CreateInstance<TType, TParameter1, TParameter2, TParameter3>(this Type type,
                                                                                     TParameter1 parameter1,
                                                                                     TParameter2 parameter2,
                                                                                     TParameter3 parameter3) {
        return CreateInstance<TType>(type, parameter1, parameter2, parameter3);
    }

    public static TType CreateInstance<TType, TParameter1, TParameter2, TParameter3, TParameter4>(this Type type,
                                                                                                  TParameter1 parameter1,
                                                                                                  TParameter2 parameter2,
                                                                                                  TParameter3 parameter3,
                                                                                                  TParameter4 parameter4) {
        return CreateInstance<TType>(type, parameter1, parameter2, parameter3, parameter4);
    }

    public static TType CreateInstance<TType>(this Type type, params object[] args) {
        return (TType)Activator.CreateInstance(type, args);
    }

    public static TType CreateGenericInstance<TType, TParameter1>(this Type type,
                                                                  Type genericType,
                                                                  TParameter1 parameter1) {
        return CreateGenericInstance<TType>(type, genericType, parameter1);
    }

    public static TType CreateGenericInstance<TType, TParameter1, TParameter2>(this Type type,
                                                                               Type genericType,
                                                                               TParameter1 parameter1,
                                                                               TParameter2 parameter2) {
        return CreateGenericInstance<TType>(type, genericType, parameter1, parameter2);
    }

    public static TType CreateGenericInstance<TType, TParameter1, TParameter2, TParameter3>(this Type type,
                                                                                            Type genericType,
                                                                                            TParameter1 parameter1,
                                                                                            TParameter2 parameter2,
                                                                                            TParameter3 parameter3) {
        return CreateGenericInstance<TType>(type, genericType, parameter1, parameter2, parameter3);
    }

    public static TType CreateGenericInstance<TType, TParameter1, TParameter2, TParameter3, TParameter4>(this Type type,
                                                                                                         Type genericType,
                                                                                                         TParameter1 parameter1,
                                                                                                         TParameter2 parameter2,
                                                                                                         TParameter3 parameter3,
                                                                                                         TParameter4 parameter4) {
        return CreateGenericInstance<TType>(type, genericType, parameter1, parameter2, parameter3, parameter4);
    }

    public static TType CreateGenericInstance<TType>(this Type type, Type genericType, params object[] args) {
        return (TType)Activator.CreateInstance(type.MakeGenericType(genericType), args);
    }

    public static void EnsurePathIsNotNull<T>(this T obj, Expression<Func<T, object>> expression) {
        var memberExpression = ExpressionUtility.ToMemberExpression(expression);

        EnsureExists(memberExpression, obj, 1);
    }

    public static IEnumerable<Type> GetAllConcreteTypesInAssemblyImplementingInterface(this Assembly assembly,
                                                                                       Type interfaceType) {
        return _typesInAssemblyByInterface.GetOrAdd((assembly, interfaceType), static key => {
            var (asm, itf) = key;

            var allMatchingTypes = new List<Type>();

            var nonGenericMatchingTypes = asm.GetTypes()
                                             .Where(t => t.ImplementsInterface(itf) &&
                                                         t.IsConcreteClass())
                                             .ToList();

            allMatchingTypes.AddRange(nonGenericMatchingTypes);

            if (itf.IsGenericType) {
                var genericInterfaceType = itf.GetGenericTypeDefinition();

                var genericTypes = asm.GetTypes()
                                      .Where(t => t.IsConcreteClass() &&
                                                  t.IsGenericType &&
                                                  t.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == genericInterfaceType))
                                      .ToList();

                if (genericTypes.Any()) {
                    var typeArguments = itf.GetGenericArguments();
                    var genericTypesWithParameters = genericTypes.Select(x => {
                                                                     try {
                                                                         return x.MakeGenericType(typeArguments);
                                                                         // Thrown when type constraints not met
                                                                     } catch (ArgumentException) {
                                                                         return null;
                                                                     }
                                                                 })
                                                                 .ExceptNull()
                                                                 .ToList();
                    allMatchingTypes.AddRange(genericTypesWithParameters);
                }
            }

            return (IEnumerable<Type>) allMatchingTypes.ToArray();
        });
    }

    public static IEnumerable<Type> GetAllConcreteTypesInAssemblyImplementingInterface(this Assembly assembly, Type interfaceType, Func<Type, bool> filter) {
        return assembly.GetAllConcreteTypesInAssemblyImplementingInterface(interfaceType)
                       .Where(filter)
                       .AsEnumerable();
    }

    public static PropertyInfo[] GetAllProperties(this Type type) {
        return type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
    }

    public static Type GetCollectionType(this Type type) {
        if (type.IsArray) {
            return type.GetElementType();
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)) {
            return type.GetGenericArguments()[0];
        }

        var genericParameters = type.GetParameterTypesForGenericInterface(typeof(IEnumerable<>));

        return genericParameters.SingleOrDefault() ?? type;
    }

    public static IEnumerable<FieldInfo> GetConstantOrStaticFields(this Type type) {
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy).ToList();

        return fields;
    }

    public static IEnumerable<T> GetConstantOrStaticValues<T>(this Type type) where T : class {
        var fields = GetConstantOrStaticFields(type);

        foreach (var field in fields) {
            if (field.IsLiteral) {
                if (field.GetRawConstantValue() is T constantValue) {
                    yield return constantValue;
                }
            } else if (field.GetValue(null) is T staticValue) {
                yield return staticValue;
            }
        }
    }

    public static IEnumerable<Type> GetParameterTypesForGenericInterface(this Type type,
                                                                         Type genericInterfaceType) {
        return _parameterTypesForGenericInterface.GetOrAdd((type, genericInterfaceType), static key => {
            var (t, genericItf) = key;

            while (t != null && t != typeof(object)) {
                Type matchingInterfaceType;

                if (t.IsInterface &&
                    t.IsGenericType &&
                    t.GetGenericTypeDefinition() == genericItf) {
                    matchingInterfaceType = t;
                } else {
                    matchingInterfaceType = t.GetInterfaces()
                                             .FirstOrDefault(x => x.IsGenericType &&
                                                                  x.GetGenericTypeDefinition() == genericItf);
                }

                if (matchingInterfaceType != null) {
                    return (IEnumerable<Type>) matchingInterfaceType.GetGenericArguments();
                }

                t = t.BaseType;
            }

            return (IEnumerable<Type>) Array.Empty<Type>();
        });
    }

    public static IReadOnlyList<Type> GetGenericParameterTypesForInheritedGenericClass(this Type type, Type genericClassType) {
        return _genericParameterTypesForInheritedGenericClass.GetOrAdd((type, genericClassType), static key => {
            var (t, genericCls) = key;

            while (t != null && t != typeof(object)) {
                if (t.IsGenericType && t.GetGenericTypeDefinition() == genericCls) {
                    return (IReadOnlyList<Type>) t.GetGenericArguments();
                }

                t = t.BaseType;
            }

            return (IReadOnlyList<Type>) Array.Empty<Type>();
        });
    }

    public static IEnumerable<Type> GetParameterTypesForGenericClass(this Type type, Type genericClassType) {
        return _parameterTypesForGenericClass.GetOrAdd((type, genericClassType), static key => {
            var (t, genericCls) = key;

            while (t != null && t != typeof(object)) {
                if (t.IsGenericType && t.GetGenericTypeDefinition() == genericCls) {
                    return (IEnumerable<Type>) t.GetGenericArguments();
                }

                t = t.BaseType;
            }

            return (IEnumerable<Type>) Array.Empty<Type>();
        });
    }

    public static string GetFriendlyName(this Type type) {
        if (type == typeof(int)) {
            return "int";
        } else if (type == typeof(short)) {
            return "short";
        } else if (type == typeof(byte)) {
            return "byte";
        } else if (type == typeof(bool)) {
            return "bool";
        } else if (type == typeof(long)) {
            return "long";
        } else if (type == typeof(float)) {
            return "float";
        } else if (type == typeof(double)) {
            return "double";
        } else if (type == typeof(decimal)) {
            return "decimal";
        } else if (type == typeof(string)) {
            return "string";
        } else if (type.IsGenericType) {
            return type.Name.Split('`')[0] + "<" + string.Join(", ", type.GetGenericArguments().Select(GetFriendlyName)) + ">";
        } else {
            return type.Name;
        }
    }

    public static PropertyInfo GetPropertyInfo(this object obj,
                                               string name,
                                               StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase) {
        var property = obj.GetType().GetAllProperties().FirstOrDefault(x => string.Equals(x.Name, name, stringComparison));

        return property;
    }

    public static PropertyInfo GetPropertyInfo<T1>(this Expression<Func<T1, object>> expr) {
        var memberExpression = ExpressionUtility.ToMemberExpression(expr);
        var propertyInfo = (PropertyInfo) memberExpression.Member;
        var subExpression = memberExpression.Expression;

        while (subExpression is MemberExpression) {
            memberExpression = (MemberExpression) subExpression;
            propertyInfo = (PropertyInfo) memberExpression.Member;
            subExpression = memberExpression.Expression;
        }

        return propertyInfo;
    }

    public static string GetPropertyPath<TModel>(this Expression<Func<TModel, object>> expr,
                                                 bool camelCase = false) {
        var asString = expr.ToString();
        var parameterName = expr.Parameters.First().Name;

        if (asString.Replace(" ", "").EqualsInvariant($"{parameterName}=>{parameterName}")) {
            return null;
        }

        var firstDelimiter = asString.IndexOf('.');

        var pathComponents = asString.Substring(firstDelimiter + 1).Split('.');

        if (camelCase) {
            pathComponents = pathComponents.Select(x => x.Camelize()).ToArray();
        }

        var result = string.Join('.', pathComponents);

        return result;
    }

    public static Action<T, TProperty> GetSetter<T, TProperty>(this Expression<Func<T, TProperty>> expr) {
        var memberExpression = ExpressionUtility.ToMemberExpression(expr);
        var property = (PropertyInfo) memberExpression.Member;

        return ReflectionUtilities.EmitPropertySetter<T, TProperty>(property);
    }

    public static Type GetValueTypeForNullableType(this Type type) {
        return _valueTypeForNullableType.GetOrAdd(type, static t => t.GetParameterTypesForGenericClass(typeof(Nullable<>)).Single());
    }

    public static bool HasAttribute<TAttribute>(this Type type) where TAttribute : Attribute {
        return type.GetCustomAttributes<TAttribute>().HasAny();
    }

    public static bool HasAttribute(this Type type, Type attributeType) {
        return type.GetCustomAttributes(attributeType).HasAny();
    }

    public static bool HasAttribute<TAttribute>(this Assembly assembly) where TAttribute : Attribute {
        return assembly.GetCustomAttributes<TAttribute>().HasAny();
    }

    public static bool HasAttribute(this Assembly assembly, Type attributeType) {
        return assembly.GetCustomAttributes(attributeType).HasAny();
    }

    public static bool HasAttribute<TAttribute>(this MethodInfo methodInfo) where TAttribute : Attribute {
        return methodInfo.GetCustomAttributes<TAttribute>().HasAny();
    }

    public static bool HasAttribute(this MethodInfo methodInfo, Type attributeType) {
        return methodInfo.GetCustomAttributes(attributeType).HasAny();
    }

    public static bool HasParameterlessConstructor(this Type type) {
        return _hasParameterlessConstructor.GetOrAdd(type, static t => t.GetConstructor([]) != null);
    }

    public static bool HasProperty(this object obj,
                                   string name,
                                   StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase) {
        return obj.GetType().GetAllProperties().Any(x => string.Equals(x.Name, name, stringComparison));
    }

    public static bool IsConcreteClass(this Type type) {
        return type.IsClass && !type.IsAbstract;
    }

    public static bool ImplementsInterface<TInterface>(this Type type) {
        return ImplementsInterface(type, typeof(TInterface));
    }

    public static bool ImplementsInterface(this Type type, Type interfaceType) {
        if (!interfaceType.IsInterface || (interfaceType.IsGenericType && !interfaceType.IsConstructedGenericType)) {
            throw new Exception($"{interfaceType.FullName.Quote()} is either not an interface or is not a constructed generic type");
        }

        return _implementsInterface.GetOrAdd((type, interfaceType), static key => key.Item2.IsAssignableFrom(key.Item1));
    }

    public static bool ImplementsGenericInterface(this Type type, Type genericInterfaceType) {
        if (!genericInterfaceType.IsGenericType || !genericInterfaceType.IsInterface) {
            throw new Exception($"{genericInterfaceType.FullName.Quote()} is not a generic interface type");
        }

        if (genericInterfaceType.IsConstructedGenericType) {
            throw new Exception($"{genericInterfaceType.FullName.Quote()} is not an open generic interface type");
        }

        return _implementsGenericInterface.GetOrAdd((type, genericInterfaceType), static key => {
            var (t, genericItf) = key;

            while (t != null && t != typeof(object)) {
                if (t.IsInterface) {
                    if (t.IsGenericType && t.GetGenericTypeDefinition() == genericItf) {
                        return true;
                    }
                } else if (t.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == genericItf)) {
                    return true;
                }

                t = t.BaseType;
            }

            return false;
        });
    }

    public static bool InheritsGenericClass(this Type type, Type genericClassType) {
        return _inheritsGenericClass.GetOrAdd((type, genericClassType), static key => {
            var (t, genericCls) = key;

            while (t != null && t != typeof(object)) {
                if (t.IsGenericType && t.GetGenericTypeDefinition() == genericCls) {
                    return true;
                }

                t = t.BaseType;
            }

            return false;
        });
    }

    public static bool IsCollectionType(this Type type) {
        return (type.IsArray || type.ImplementsGenericInterface(typeof(IEnumerable<>))) && type != typeof(string);
    }

    public static bool IsLookup(this Type type) {
        return type.ImplementsInterface<ILookup>();
    }

    public static bool IsNullableType(this Type type) {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
    }

    public static bool IsOfTypeOrNullableType<T>(this Type type) where T : struct {
        return type == typeof(T) || type == typeof(T?);
    }

    public static bool IsSystemType(this Type type) {
        return type.Namespace.StartsWith("System");
    }

    public static bool IsSubclassOfType(this Type type, Type otherType) {
        return _isSubclassOfType.GetOrAdd((type, otherType), static key => key.Item1.IsSubclassOf(key.Item2));
    }

    public static bool IsSubclassOrSubInterfaceOfGenericType(this Type type, Type genericType) {
        return _isSubclassOrSubInterfaceOfGenericType.GetOrAdd((type, genericType), static key => {
            var (t, generic) = key;

            if (!generic.IsGenericType) {
                throw new Exception($"{generic.FullName.Quote()} is not a generic type");
            }

            while (t != null && t != typeof(object)) {
                var currentType = t.IsGenericType ? t.GetGenericTypeDefinition() : t;

                if (generic == currentType) {
                    return true;
                }

                t = t.BaseType;
            }

            return false;
        });
    }

    public static void SetPropertyValue(this object obj, string name, object value, StringComparison stringComparison = StringComparison.InvariantCultureIgnoreCase) {
        obj.GetPropertyInfo(name, stringComparison).SetValue(obj, value);
    }

    private static object EnsureExists(MemberExpression node, object target, int depth) {
        if (node.Expression is MemberExpression innerExpression) {
            target = EnsureExists(innerExpression, target, depth + 1);
        }

        if (depth > 1) {
            var propertyInfo = (PropertyInfo) node.Member;
            var propertyType = propertyInfo.PropertyType;
            var propertyValue = propertyInfo.GetValue(target) ?? Activator.CreateInstance(propertyType);

            propertyInfo.SetValue(target, propertyValue);

            return propertyValue;
        }

        return target;
    }

    private static MissingMethodException MethodNotFound(string methodName, Type type) {
        return new MissingMethodException(type.FullName, methodName);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Caching strategy — read before adding/changing entries below
    // ──────────────────────────────────────────────────────────────────────────
    // Reflection results are immutable for the process lifetime, so caching is
    // purely a performance optimisation — no invalidation logic, no expiry.
    //
    // Design:
    //   * One typed ConcurrentDictionary per cached method. Keys are Type or
    //     (Type, Type) — no string allocation on the hot path.
    //   * Single-Type-keyed caches are unbounded ConcurrentDictionary. The
    //     keyspace ceiling is the set of "Our" exported types, so memory is
    //     bounded by construction.
    //   * Composite-keyed caches use BoundedCache with a defensive ~50k cap.
    //     Cap exists to protect against a buggy caller exposing a huge
    //     cross-product, not as an expected hit point.
    //   * Behaviour on cap overflow: the cache stops accepting new entries
    //     and the call falls through to uncached evaluation. Correct result,
    //     just slower for the unlucky later-frequented key.
    //   * Methods whose underlying check is sub-100ns are not cached at all
    //     — a ConcurrentDictionary.TryGetValue costs more than the check itself.
    //     Examples: IsConcreteClass, IsOfTypeOrNullableType, IsLookup,
    //     IsNullableType.
    //
    // Adding a new cached helper:
    //   * Declare a typed cache field below.
    //   * Pick BoundedCache iff the keyspace could exceed the single-Type
    //     ceiling — i.e. composite keys built from caller-supplied Types.
    //   * Don't introduce string keys; route through Type refs.
    // ──────────────────────────────────────────────────────────────────────────

    private class BoundedCache<TKey, TValue> {
        private const int DefaultCapacity = 50_000;

        private readonly ConcurrentDictionary<TKey, TValue> _map = new();
        private readonly int _capacity;
        private int _count;

        public BoundedCache(int capacity = DefaultCapacity) {
            _capacity = capacity;
        }

        public TValue GetOrAdd(TKey key, Func<TKey, TValue> factory) {
            if (_map.TryGetValue(key, out var existing)) {
                return existing;
            }

            var value = factory(key);

            if (Volatile.Read(ref _count) < _capacity) {
                if (_map.TryAdd(key, value)) {
                    Interlocked.Increment(ref _count);
                }
            }

            return value;
        }
    }

    // Single-Type-keyed caches — unbounded (ceiling ≈ Our exported types).
    private static readonly ConcurrentDictionary<Type, bool> _hasParameterlessConstructor = new();
    private static readonly ConcurrentDictionary<Type, Type> _valueTypeForNullableType = new();

    // Two-Type-keyed caches — bounded (defensive 50k cap).
    private static readonly BoundedCache<(Type, Type), bool> _implementsInterface = new();
    private static readonly BoundedCache<(Type, Type), bool> _implementsGenericInterface = new();
    private static readonly BoundedCache<(Type, Type), bool> _inheritsGenericClass = new();
    private static readonly BoundedCache<(Type, Type), bool> _isSubclassOfType = new();
    private static readonly BoundedCache<(Type, Type), bool> _isSubclassOrSubInterfaceOfGenericType = new();
    private static readonly BoundedCache<(Type, Type), IEnumerable<Type>> _parameterTypesForGenericInterface = new();
    private static readonly BoundedCache<(Type, Type), IEnumerable<Type>> _parameterTypesForGenericClass = new();
    private static readonly BoundedCache<(Type, Type), IReadOnlyList<Type>> _genericParameterTypesForInheritedGenericClass = new();

    // (Assembly, Type)-keyed cache — bounded with a tighter cap (assemblies are few).
    private static readonly BoundedCache<(Assembly, Type), IEnumerable<Type>> _typesInAssemblyByInterface = new(capacity: 5_000);

    public class MethodCallBuilder {
        private readonly Type _targetType;
        private readonly object _targetInstance;
        private readonly string _methodName;
        private readonly List<Type> _genericTypes = [];
        private readonly List<Type> _parameterTypes = [];
        private readonly List<object> _parameters = [];

        public MethodCallBuilder(Type targetType, object targetInstance, string methodName) {
            _targetType = targetType;
            _targetInstance = targetInstance;
            _methodName = methodName;
        }

        public MethodCallBuilder OfGenericType<T>() {
            return OfGenericType(typeof(T));
        }

        public MethodCallBuilder OfGenericType(Type type) {
            _genericTypes.Add(type);

            return this;
        }

        public MethodCallBuilder WithParameter(Type type, object parameter) {
            _parameterTypes.Add(type);
            _parameters.Add(parameter);

            return this;
        }

        public Task RunAsync() {
            return (Task)Run();
        }

        public Task<T> RunAsync<T>() {
            return (Task<T>)RunAsync();
        }

        public T Run<T>() {
            return (T)Run();
        }

        public object Run() {
            var method = FindMethod(_targetType, _methodName, _parameterTypes, _genericTypes);

            if (method is null) {
                throw MethodNotFound(_methodName, _targetType);
            }

            return method.Invoke(_targetInstance, _parameters.ToArray());
        }

        // Not cached: composite key of (Type, string, IReadOnlyList<Type>,
        // IReadOnlyList<Type>) doesn't have a cheap value-equal representation,
        // and this builder is called infrequently (one-off reflective dispatch).
        private static MethodInfo FindMethod(Type classType, string methodName, IReadOnlyList<Type> parameterTypes, IReadOnlyList<Type> genericTypes) {
            var genericParameterTypes = parameterTypes.Select(p => p.IsGenericType ? p.GetGenericTypeDefinition() : p).ToList();
            var numberOfGenerics = genericTypes.Count;
            var candidateMethods = new List<MethodInfo>();

            var type = classType;
            while (type != null && type != typeof(object)) {
                var allMethods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.FlattenHierarchy).ToList();
                var methodsMatchingNameAndParameterCount = allMethods.Where(m => m.Name.EqualsInvariant(methodName) &&
                                                                                 m.GetParameters().Length == parameterTypes.Count)
                                                                     .ToList();

                candidateMethods.AddRange(methodsMatchingNameAndParameterCount.Where(m => numberOfGenerics == 0 || m.IsGenericMethod && m.GetGenericArguments().Length == numberOfGenerics).AsEnumerable());

                type = type.BaseType;
            }

            foreach (var method in candidateMethods) {
                var concreteMethod = genericTypes.Any() ? method.MakeGenericMethod(genericTypes.ToArray()) : method;

                var methodParameterTypes = concreteMethod.GetParameters().Select(p => p.ParameterType.IsGenericType ? p.ParameterType.GetGenericTypeDefinition() : p.ParameterType).ToList();

                if (methodParameterTypes.SequenceEqual(genericParameterTypes)) {
                    return concreteMethod;
                }
            }

            return null;
        }
    }
}
