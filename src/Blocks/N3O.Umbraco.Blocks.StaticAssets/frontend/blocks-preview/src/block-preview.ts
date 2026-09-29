import { customElement } from '@umbraco-cms/backoffice/external/lit';
import { UMB_BLOCK_ENTRY_CONTEXT, UMB_BLOCK_MANAGER_CONTEXT } from '@umbraco-cms/backoffice/block';
import type { UmbBlockEditorCustomViewElement } from '@umbraco-cms/backoffice/block-custom-view';
import { UMB_DOCUMENT_WORKSPACE_CONTEXT } from '@umbraco-cms/backoffice/document';
import { UMB_PROPERTY_CONTEXT } from '@umbraco-cms/backoffice/property';

import { UmbAuthFetchMixin, UmbElementMixin } from '@n3oltd/backoffice-core';
import type { AuthFetch } from '@n3oltd/backoffice-core';

import { createElement } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { BlockPreviewApp } from './block-preview-app';
import { coordinatorFor, type PreviewCoordinator } from './preview-coordinator';
import type { PreviewEntry, PreviewState } from './types';

const elementName = 'n3o-block-preview';

// The editor scrolls inside a nested container, which rootMargin does not extend into, so the prefetch distance
// has to be a scrollMargin. TypeScript's DOM typings do not declare it yet.
const observerOptions: IntersectionObserverInit & { scrollMargin: string } = { scrollMargin: '400px' };

const hostStyles = `
    :host { display: block; }

    umb-block-grid-areas-container::part(area) { margin: var(--uui-size-2); }
`;

@customElement(elementName)
export class N3oBlockPreviewElement extends UmbAuthFetchMixin(UmbElementMixin(HTMLElement))
    implements UmbBlockEditorCustomViewElement, PreviewEntry {
    #content?: UmbBlockEditorCustomViewElement['content'];
    #settings?: UmbBlockEditorCustomViewElement['settings'];
    #layout?: UmbBlockEditorCustomViewElement['layout'];

    get content(): UmbBlockEditorCustomViewElement['content'] {
        return this.#content;
    }

    set content(value: UmbBlockEditorCustomViewElement['content']) {
        this.#content = value;
        this.#onDataChanged();
    }

    get settings(): UmbBlockEditorCustomViewElement['settings'] {
        return this.#settings;
    }

    set settings(value: UmbBlockEditorCustomViewElement['settings']) {
        this.#settings = value;
        this.#onDataChanged();
    }

    // The preview is rendered from the whole grid value, so the block's spans change it as much as its content.
    get layout(): UmbBlockEditorCustomViewElement['layout'] {
        return this.#layout;
    }

    set layout(value: UmbBlockEditorCustomViewElement['layout']) {
        this.#layout = value;
        this.#onDataChanged();
    }

    #root?: Root;
    #mount: HTMLDivElement;
    #areas: HTMLElement;
    #state: PreviewState = { status: 'loading' };

    #coordinator?: PreviewCoordinator;
    #observer?: IntersectionObserver;
    #visible = false;

    #nodeKey: string | null = null;
    #documentTypeKey: string | null = null;
    #propertyAlias: string | null = null;
    #culture = '';

    contentKey = '';

    constructor() {
        super();

        const shadow = this.attachShadow({ mode: 'open' });
        this.#mount = document.createElement('div');
        shadow.appendChild(this.#mount);

        // A custom view replaces the block card, which is what carries the container a block's areas are edited
        // through. The container renders nothing for a block without areas.
        this.#areas = document.createElement('umb-block-grid-areas-container');
        this.#areas.setAttribute('draggable', 'false');
        shadow.appendChild(this.#areas);

        const style = document.createElement('style');
        style.textContent = hostStyles;
        shadow.appendChild(style);

        this.consumeContext(UMB_DOCUMENT_WORKSPACE_CONTEXT, (context) => {
            if (!context) {
                return;
            }

            this.observe(context.unique, (unique) => {
                this.#nodeKey = unique ?? null;
                this.#pushContext();
            }, '_observeUnique');

            // A never-published document is previewed against another document of the same type.
            this.observe(context.contentTypeUnique, (unique) => {
                this.#documentTypeKey = unique ?? null;
                this.#pushContext();
            }, '_observeContentType');

            this.observe(context.splitView.activeVariantsInfo, (infos) => {
                this.#culture = infos[0]?.culture ?? '';
                this.#pushContext();
            }, '_observeCulture');
        });

        this.consumeContext(UMB_PROPERTY_CONTEXT, (context) => {
            if (!context) {
                return;
            }

            this.observe(context.alias, (alias) => {
                this.#propertyAlias = alias ?? null;
                this.#pushContext();
            }, '_observePropertyAlias');
        });

        this.consumeContext(UMB_BLOCK_ENTRY_CONTEXT, (context) => {
            if (!context) {
                return;
            }

            this.observe(context.contentKey, (key) => {
                if (key === this.contentKey) {
                    return;
                }

                this.#coordinator?.unregister(this);
                this.contentKey = key ?? '';
                this.#join();
            }, '_observeContentKey');
        });

        this.consumeContext(UMB_BLOCK_MANAGER_CONTEXT, (context) => {
            const coordinator = context ? coordinatorFor(context) : undefined;

            if (coordinator === this.#coordinator) {
                return;
            }

            this.#coordinator?.unregister(this);
            this.#coordinator = coordinator;
            this.#join();
        });
    }

    fingerprint(): string {
        return JSON.stringify([this.#content, this.#settings, this.#layout]);
    }

    receive(state: PreviewState): void {
        this.#state = state;
        this.#render();
    }

    authFetchChanged(authFetch: AuthFetch | null): void {
        this.#coordinator?.setAuthFetch(authFetch);
        this.#requestIfVisible(0);
    }

    connectedCallback(): void {
        super.connectedCallback();

        this.#root ??= createRoot(this.#mount);
        this.#render();
        this.#revealActions();

        // Sorting a block disconnects and reconnects the same instance after its contexts have resolved, so
        // nothing else would register it with the coordinator again.
        this.#join();

        this.#observer ??= new IntersectionObserver((entries) => {
            this.#visible = entries.some((x) => x.isIntersecting);

            if (this.#visible) {
                this.#requestIfVisible(0);
            }
        }, observerOptions);

        this.#observer.observe(this);
    }

    disconnectedCallback(): void {
        super.disconnectedCallback();

        this.#observer?.disconnect();
        this.#observer = undefined;

        this.#coordinator?.unregister(this);

        this.#root?.unmount();
        this.#root = undefined;
    }

    #join(): void {
        if (!this.#coordinator || !this.contentKey) {
            return;
        }

        this.#coordinator.register(this);
        this.#coordinator.setAuthFetch(this.authFetch);
        this.#pushContext();
        this.#requestIfVisible(0);
    }

    #pushContext(): void {
        this.#coordinator?.setContext({
            nodeKey: this.#nodeKey,
            documentTypeKey: this.#documentTypeKey,
            propertyAlias: this.#propertyAlias,
            culture: this.#culture,
        });
    }

    #onDataChanged(): void {
        this.#requestIfVisible();
    }

    #requestIfVisible(delay?: number): void {
        if (this.#visible && this.contentKey) {
            this.#coordinator?.request(this, delay);
        }
    }

    // The action bar's opacity variable belongs to umb-block-grid-entry, which is several shadow roots up because
    // the view is mounted through an extension slot.
    #revealActions(): void {
        let node: Node = this;

        while (true) {
            const root = node.getRootNode();
            const host = root instanceof ShadowRoot ? root.host : null;

            if (!(host instanceof HTMLElement)) {
                return;
            }

            if (host.tagName.toLowerCase() === 'umb-block-grid-entry') {
                host.style.setProperty('--umb-block-grid-entry-actions-opacity', '1');

                return;
            }

            node = host;
        }
    }

    #render(): void {
        this.#root?.render(createElement(BlockPreviewApp, { state: this.#state }));
    }
}

export default N3oBlockPreviewElement;

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: N3oBlockPreviewElement;
    }
}
