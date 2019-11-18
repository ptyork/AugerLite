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

        public List<W3CHtmlValidationMessage> W3CHtmlValidationMessages { get; set; } = new List<W3CHtmlValidationMessage>();
        public List<W3CHtmlValidationMessage> W3CCssValidationMessagesNew { get; set; } = new List<W3CHtmlValidationMessage>();

        public List<W3CCssValidationMessage> W3CCssValidationMessages { get; set; } = new List<W3CCssValidationMessage>();

        public DOMTestStats Stats { get; set; } = new DOMTestStats();
        public HashSet<DOMTest> Tests { get; set; } = new HashSet<DOMTest>();
        public HashSet<DOMTest> Passes { get; set; } = new HashSet<DOMTest>();
        public HashSet<DOMTest> Pending { get; set; } = new HashSet<DOMTest>();
        public HashSet<DOMTest> Failures { get; set; } = new HashSet<DOMTest>();

        public List<string> Exceptions { get; set; } = new List<string>();
        public List<String> DebugMessages { get; set; } = new List<String>();

        public void AppendResults(TestResults r)
        {
            this.Exceptions.AddRange(r.Exceptions);

            this.W3CHtmlValidationMessages.AddRange(r.W3CHtmlValidationMessages);
            this.HtmlValidationCompleted = this.HtmlValidationCompleted || r.HtmlValidationCompleted;

            this.W3CCssValidationMessagesNew.AddRange(r.W3CCssValidationMessagesNew);

            this.W3CCssValidationMessages.AddRange(r.W3CCssValidationMessages);
            this.CssValidationCompleted = this.CssValidationCompleted || r.CssValidationCompleted;

            this.DomTestComplete = this.DomTestComplete || r.DomTestComplete;

            this.Stats.Tests += r.Stats.Tests;
            this.Stats.Passes += r.Stats.Passes;
            this.Stats.Pending += r.Stats.Pending;
            this.Stats.Failures += r.Stats.Failures;
            this.Stats.Duration += r.Stats.Duration;

            foreach (var item in r.Tests) this.Tests.Add(item);
            foreach (var item in r.Passes) this.Passes.Add(item);
            foreach (var item in r.Pending) this.Pending.Add(item);
            foreach (var item in r.Failures) this.Failures.Add(item);

            this.DebugMessages.AddRange(r.DebugMessages);
        }
    }
}
