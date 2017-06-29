using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Auger.Models
{
    public class TestResults
    {
        public bool HtmlValidationCompleted { get; set; } = false;
        public bool CssValidationCompleted { get; set; } = false;
        public bool DomTestComplete { get; set; } = false;

        public List<string> Exceptions { get; set; } = new List<string>();

        public List<W3CHtmlValidationMessage> W3CHtmlValidationMessages { get; set; } = new List<W3CHtmlValidationMessage>();

        public List<W3CCssValidationMessage> W3CCssValidationMessages { get; set; } = new List<W3CCssValidationMessage>();

        public DOMTestStats Stats { get; set; } = new DOMTestStats();
        public List<DOMTest> Tests { get; set; } = new List<DOMTest>();
        public List<DOMTest> Passes { get; set; } = new List<DOMTest>();
        public List<DOMTest> Pending { get; set; } = new List<DOMTest>();
        public List<DOMTest> Failures { get; set; } = new List<DOMTest>();

        public void AppendResults(TestResults r)
        {
            this.Exceptions.AddRange(r.Exceptions);

            this.W3CHtmlValidationMessages.AddRange(r.W3CHtmlValidationMessages);
            this.HtmlValidationCompleted = this.HtmlValidationCompleted || r.HtmlValidationCompleted;

            this.W3CCssValidationMessages.AddRange(r.W3CCssValidationMessages);
            this.CssValidationCompleted = this.CssValidationCompleted || r.CssValidationCompleted;

            this.DomTestComplete = this.DomTestComplete || r.DomTestComplete;

            this.Stats.Tests += r.Stats.Tests;
            this.Stats.Passes += r.Stats.Passes;
            this.Stats.Pending += r.Stats.Pending;
            this.Stats.Failures += r.Stats.Failures;
            this.Stats.Duration += r.Stats.Duration;

            this.Tests.AddRange(r.Tests);

            this.Passes.AddRange(r.Passes);

            this.Pending.AddRange(r.Pending);

            this.Failures.AddRange(r.Failures);
        }
    }
}
