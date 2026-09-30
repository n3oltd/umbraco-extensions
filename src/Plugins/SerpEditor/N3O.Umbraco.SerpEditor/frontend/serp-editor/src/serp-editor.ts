import { customElement } from '@umbraco-cms/backoffice/external/lit';
import { UmbElementMixin } from '@umbraco-cms/backoffice/element-api';
import {
    type UmbPropertyEditorConfigCollection,
    type UmbPropertyEditorUiElement,
} from '@umbraco-cms/backoffice/property-editor';
import { UmbChangeEvent } from '@umbraco-cms/backoffice/event';
import {
    UMB_DOCUMENT_WORKSPACE_CONTEXT,
    UmbDocumentUrlRepository,
    UmbDocumentUrlsDataResolver,
} from '@umbraco-cms/backoffice/document';
import { observeMultiple } from '@umbraco-cms/backoffice/observable-api';
import { UmbAuthFetchMixin } from '@n3oltd/backoffice-core';
import type { AuthFetch } from '@n3oltd/backoffice-core';
import { createElement } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { SerpEditorApp, type SerpValue } from './serp-editor-app';

const elementName = 'n3o-serp-editor';

@customElement(elementName)
export class N3oSerpEditorElement extends UmbAuthFetchMixin(UmbElementMixin(HTMLElement)) implements UmbPropertyEditorUiElement {
    #root?: Root;
    #mount: HTMLDivElement;
    #value: SerpValue = { title: '', description: '' };
    #maxCharsTitle = 60;
    #maxCharsDescription = 160;
    #urlRepository = new UmbDocumentUrlRepository(this);
    #urlsDataResolver = new UmbDocumentUrlsDataResolver(this);
    #isNew = false;
    #unique: string | null | undefined;
    #url = '';

    constructor() {
        super();
        const shadow = this.attachShadow({ mode: 'open' });
        this.#mount = document.createElement('div');
        shadow.appendChild(this.#mount);

        this.consumeContext(UMB_DOCUMENT_WORKSPACE_CONTEXT, (context) => {
            if (!context) {
                return;
            }

            this.observe(observeMultiple([context.isNew, context.unique]), ([isNew, unique]) => {
                this.#isNew = isNew === true;
                this.#unique = unique;

                void this.#loadUrls();
            }, '_observeWorkspaceState');
        });

        // The resolver reads the variant context provided by the property dataset, so it yields the urls for
        // the culture this editor is rendering.
        this.observe(this.#urlsDataResolver.urls, (urls) => {
            this.#url = this.#toAbsoluteUrl(urls.at(0)?.url);
            this.#render();
        }, '_observeUrls');
    }

    async #loadUrls(): Promise<void> {
        this.#urlsDataResolver.setData([]);

        if (this.#isNew || !this.#unique) {
            return;
        }

        const { data } = await this.#urlRepository.requestItems([this.#unique]);

        this.#urlsDataResolver.setData(data?.at(0)?.urls ?? []);
    }

    #toAbsoluteUrl(url: string | undefined): string {
        if (!url) {
            return '';
        }

        if (url.startsWith('http://') || url.startsWith('https://')) {
            return url;
        }

        return `${window.location.protocol}//${window.location.hostname}${url}`;
    }

    get value(): SerpValue {
        return this.#value;
    }

    set value(value: SerpValue | undefined) {
        this.#value = value ?? { title: '', description: '' };
        this.#render();
    }

    public set config(config: UmbPropertyEditorConfigCollection | undefined) {
        const maxCharsTitle = Number.parseInt(String(config?.getValueByAlias('maxCharsTitle') ?? ''), 10);
        const maxCharsDescription = Number.parseInt(String(config?.getValueByAlias('maxCharsDescription') ?? ''),
                                                    10);

        if (!Number.isNaN(maxCharsTitle) && maxCharsTitle > 0) {
            this.#maxCharsTitle = maxCharsTitle;
        }

        if (!Number.isNaN(maxCharsDescription) && maxCharsDescription > 0) {
            this.#maxCharsDescription = maxCharsDescription;
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

    #render(): void {
        this.#root?.render(
            createElement(SerpEditorApp, {
                value: this.#value,
                url: this.#url,
                maxCharsTitle: this.#maxCharsTitle,
                maxCharsDescription: this.#maxCharsDescription,
                authFetch: this.authFetch,
                onChange: (value: SerpValue) => {
                    this.#value = value;
                    this.dispatchEvent(new UmbChangeEvent());
                },
            }),
        );
    }
}

export default N3oSerpEditorElement;

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: N3oSerpEditorElement;
    }
}
