import { customElement, html, nothing } from '@umbraco-cms/backoffice/external/lit';
import { UeSegmentRuleBaseElement } from '@umbraco-engage/backoffice/personalization';

const elementName = 'segment-rule-telethon-on-air-editor';

@customElement(elementName)
export class SegmentRuleTelethonOnAirEditorElement extends UeSegmentRuleBaseElement {
    override connectedCallback(): void {
        super.connectedCallback();

        // Save does nothing without a pending value, and this rule has no parameters to set one.
        this.pending ??= this.value;
    }

    renderReadOnly() {
        return html`${this.manifest?.meta.name}`;
    }

    renderEditor() {
        return nothing;
    }
}

export default SegmentRuleTelethonOnAirEditorElement;

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: SegmentRuleTelethonOnAirEditorElement;
    }
}
