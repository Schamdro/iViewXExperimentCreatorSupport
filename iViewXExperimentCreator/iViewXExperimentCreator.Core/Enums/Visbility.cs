using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iViewXExperimentCreator.Core.Enums
{
    /// <summary>
    /// Plattformagnostische Sichtbarkeitsflags für UI-Elemente. 
    /// 
    /// Im View sollte ein ValueConverter existieren, der AgnosticVisibility 
    /// in plattformspezifische Visibility-Werte umwandelt.
    /// </summary>
    public enum AgnosticVisibility
    {
        Visible,
        Hidden,
        Collapsed
    }
}
