import { customElement } from '@umbraco-cms/backoffice/external/lit';
import { UmbElementMixin } from '@umbraco-cms/backoffice/element-api';
import type { UmbPropertyEditorUiElement } from '@umbraco-cms/backoffice/property-editor';
import { UmbChangeEvent } from '@umbraco-cms/backoffice/event';
import { UmbAuthFetchMixin } from '@n3oltd/backoffice-core';
import type { AuthFetch } from '@n3oltd/backoffice-core';
import { createElement } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { CampaignTagsApp } from './campaign-tags-app';
import { isSameValue, toRows, toValue, type CampaignTag } from './campaign-tags-value';

const elementName = 'n3o-campaign-tags';

@customElement(elementName)
export class N3oCampaignTagsElement
    extends UmbAuthFetchMixin(UmbElementMixin(HTMLElement))
    implements UmbPropertyEditorUiElement {
    #root?: Root;
    #mount: HTMLDivElement;
    #rows: CampaignTag[] = [];

    constructor() {
        super();
        const shadow = this.attachShadow({ mode: 'open' });
        this.#mount = document.createElement('div');
        shadow.appendChild(this.#mount);
    }

    get value(): CampaignTag[] | undefined {
        return toValue(this.#rows);
    }

    set value(value: CampaignTag[] | undefined) {
        if (!isSameValue(this.#rows, value)) {
            this.#rows = toRows(value);
        }

        this.#render();
    }

    authFetchChanged(_authFetch: AuthFetch | null): void {
        this.#render();
    }

    connectedCallback(): void {
        super.connectedCallback?.();
        this.#root ??= createRoot(this.#mount);
        this.#render();
    }

    disconnectedCallback(): void {
        super.disconnectedCallback?.();
        this.#root?.unmount();
        this.#root = undefined;
    }

    #onChange(rows: CampaignTag[]): void {
        const isChanged = !isSameValue(rows, toValue(this.#rows));

        this.#rows = rows;
        this.#render();

        if (isChanged) {
            this.dispatchEvent(new UmbChangeEvent());
        }
    }

    #render(): void {
        this.#root?.render(
            createElement(CampaignTagsApp, {
                rows: this.#rows,
                authFetch: this.authFetch,
                onChange: (rows: CampaignTag[]) => this.#onChange(rows),
            }),
        );
    }
}

export default N3oCampaignTagsElement;

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: N3oCampaignTagsElement;
    }
}
