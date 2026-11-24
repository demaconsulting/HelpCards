// Views rendered into docs/design/generated/ for the Design document.
// Per SysML2Tools syntax, 'expose' is only valid inside a named 'view' usage
// (not a 'view def' definition), and it - not 'render' - is what scopes the
// rendered diagram content.
package HelpCardsDesignViews {
    private import StandardViewDefinitions::*;
    private import HelpCards::*;

    view SoftwareStructureView : InterconnectionView {
        expose HelpCards::HelpCardsSystem;
    }

    view HelpCardsView {
        expose HelpCardsSystem;
    }

    view LoadingView {
        expose LoadingSubsystem;
    }

    view CompositionView {
        expose CompositionSubsystem;
    }

    view CatalogView {
        expose CatalogSubsystem;
    }

    view PresentationView {
        expose PresentationSubsystem;
    }

    view TestingView {
        expose TestingSubsystem;
    }
}
