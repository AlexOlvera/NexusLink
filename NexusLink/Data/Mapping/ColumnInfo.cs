using System;
/// <summary>
/// Información de columna para mapeo de entidades
/// </summary>
public class ColumnInfo
{
    /// <summary>
    /// Nombre de la columna
    /// </summary>
    public string ColumnName { get; set; }

    /// <summary>
    /// Tipo de la propiedad
    /// </summary>
    public Type PropertyType { get; set; }

    /// <summary>
    /// Indica si la columna es clave primaria
    /// </summary>
    public bool IsPrimaryKey { get; set; }

    /// <summary>
    /// Indica si la columna es clave foránea
    /// </summary>
    public bool IsForeignKey { get; set; }

    /// <summary>
    /// Indica si la columna es clave única
    /// </summary>
    public bool IsUniqueKey { get; set; }

    /// <summary>
    /// Indica si la columna es requerida (NOT NULL)
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Indica si la columna se agrega automáticamente (IDENTITY)
    /// </summary>
    public bool IsIdentity { get; set; }

    /// <summary>
    /// Indica si la columna es criterial (se usa para búsquedas)
    /// </summary>
    public bool IsCriterial { get; set; }

    /// <summary>
    /// Indica si la columna es calculada (COMPUTED)
    /// </summary>
    public bool IsComputed { get; set; }

    /// <summary>
    /// Nombre de la propiedad relacionada (si es clave foránea)
    /// </summary>
    public string RelatedProperty { get; set; }

    /// <summary>
    /// Tabla de referencia (si es clave foránea)
    /// </summary>
    public string ReferenceTable { get; set; }

    /// <summary>
    /// Columna de referencia (si es clave foránea)
    /// </summary>
    public string ReferenceColumn { get; set; }
}