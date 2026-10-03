import { customElement } from '@umbraco-cms/backoffice/external/lit';
import { UmbElementMixin } from '@umbraco-cms/backoffice/element-api';
import { UmbChangeEvent } from '@umbraco-cms/backoffice/event';
import type {
    UmbPropertyEditorConfigCollection,
    UmbPropertyEditorUiElement,
} from '@umbraco-cms/backoffice/property-editor';
import { UMB_MODAL_MANAGER_CONTEXT, type UmbModalManagerContext } from '@umbraco-cms/backoffice/modal';
import { UMB_MEDIA_PICKER_MODAL, UmbMediaUrlRepository, UmbMediaItemRepository } from '@umbraco-cms/backoffice/media';
import { UMB_LINK_PICKER_MODAL } from '@umbraco-cms/backoffice/multi-url-picker';
import { imageSize } from '@umbraco-cms/backoffice/utils';
import type { MediaPickerResultItem } from './tools/UmbracoImageTool';
import type { EditorJsFrameConfig } from './editor-js-frame';

const elementName = 'n3o-editor-js';

type EditorJsFrame = HTMLIFrameElement & {
    __n3oInit?: (config: EditorJsFrameConfig) => void;
    __n3oSetValue?: (value: string | undefined) => void;
    __n3oOnReady?: () => void;
};

interface MediaPickerModalResult {
    selection: Array<string | null>;
}

@customElement(elementName)
export class N3oEditorJsElement
    extends UmbElementMixin(HTMLElement)
    implements UmbPropertyEditorUiElement
{
    #iframe?: EditorJsFrame;
    #value: string | undefined;
    #resizeObserver?: ResizeObserver;
    #modalManager?: UmbModalManagerContext;

    constructor() {
        super();

        this.consumeContext(UMB_MODAL_MANAGER_CONTEXT, (context) => {
            this.#modalManager = context;
        });
    }

    get value(): string | undefined {
        return this.#value;
    }

    set value(value: string | undefined) {
        if (value === this.#value) {
            return;
        }

        this.#value = value;
        this.#iframe?.__n3oSetValue?.(value);
    }

    public set config(_config: UmbPropertyEditorConfigCollection | undefined) {}

    connectedCallback(): void {
        super.connectedCallback();
        if (!this.#iframe) {
            this.#createFrame();
        }
    }

    disconnectedCallback(): void {
        super.disconnectedCallback();
        this.#resizeObserver?.disconnect();
        this.#resizeObserver = undefined;
        // Iframes reload their content when re-parented.
        this.#iframe?.remove();
        this.#iframe = undefined;
    }

    #createFrame(): void {
        const iframe = document.createElement('iframe') as EditorJsFrame;
        iframe.setAttribute('allow', 'fullscreen');
        iframe.style.width = '100%';
        iframe.style.border = '0';
        iframe.style.display = 'block';
        iframe.style.minHeight = '300px';
        iframe.__n3oOnReady = () => this.#onFrameReady();

        this.appendChild(iframe);
        this.#iframe = iframe;

        const doc = iframe.contentDocument;
        if (!doc) {
            this.#showFrameFailure();

            return;
        }
        doc.open();
        doc.write('<!doctype html><html><head><meta charset="utf-8"></head><body></body></html>');
        doc.close();

        const script = doc.createElement('script');
        script.type = 'module';
        // Vite rewrites new URL('literal', import.meta.url) at build time into an inlined asset.
        script.src = import.meta.url.replace('editor-js.js', 'editor-js-frame.js');
        script.onerror = () => this.#showFrameFailure();
        doc.head.appendChild(script);
    }

    #showFrameFailure(): void {
        const message = document.createElement('p');
        message.textContent = 'The editor could not be loaded. Reload the page to try again.';

        this.appendChild(message);

        console.error('[EditorJs] The editor frame failed to load.');
    }

    #onFrameReady(): void {
        const iframe = this.#iframe;
        if (!iframe?.__n3oInit) {
            return;
        }

        iframe.__n3oInit({
            value: this.#value,
            onChange: (value: string) => {
                this.#value = value;
                this.dispatchEvent(new UmbChangeEvent());
            },
            pickMedia: () => this.#pickMedia(),
            pickLink: () => this.#pickLink(),
            requestSave: (key: string) => this.#requestSave(key),
        });

        const body = iframe.contentDocument?.body;
        if (body) {
            const resize = () => {
                iframe.style.height = `${body.scrollHeight}px`;
            };
            this.#resizeObserver = new ResizeObserver(resize);
            this.#resizeObserver.observe(body);
            resize();
        }
    }

    // The backoffice binds its save shortcut to the parent window.
    #requestSave(key: string): void {
        window.dispatchEvent(
            new KeyboardEvent('keydown', { key, metaKey: true, ctrlKey: true, bubbles: true }),
        );
    }

    async #pickMedia(): Promise<MediaPickerResultItem | null> {
        const modalManager = this.#modalManager;
        if (!modalManager) {
            return null;
        }

        const modal = modalManager.open(this, UMB_MEDIA_PICKER_MODAL, { data: { multiple: false } });

        try {
            const result = (await modal.onSubmit()) as MediaPickerModalResult;
            const unique = result?.selection?.find((s) => s != null) ?? null;
            if (!unique) {
                return null;
            }

            const urlRepo = new UmbMediaUrlRepository(this);
            const itemRepo = new UmbMediaItemRepository(this);
            try {
                const [urlResult, itemResult] = await Promise.all([
                    urlRepo.requestItems([unique]),
                    itemRepo.requestItems([unique]),
                ]);

                const url = urlResult.data?.[0]?.url;

                // A block with no URL is dropped on save rather than reported.
                if (!url) {
                    return null;
                }

                // A media file that fails to load is still picked, without dimensions.
                const size = await imageSize(url).catch(() => undefined);

                return {
                    url,
                    name: itemResult.data?.[0]?.name ?? '',
                    unique,
                    // Umbraco's Udi expects umb://media/<guid> in dash-less form.
                    udi: `umb://media/${unique.replace(/-/g, '')}`,
                    width: size?.naturalWidth,
                    height: size?.naturalHeight,
                };
            } finally {
                urlRepo.destroy();
                itemRepo.destroy();
            }
        } catch {
            return null;
        }
    }

    async #pickLink(): Promise<string | null> {
        const modalManager = this.#modalManager;
        if (!modalManager) {
            return null;
        }

        const modal = modalManager.open(this, UMB_LINK_PICKER_MODAL, {
            data: { config: {}, index: null, isNew: true },
        });

        try {
            const result = await modal.onSubmit();
            const link = result?.link;

            if (!link) {
                return null;
            }

            // An internal link is stored as its UDI so it keeps resolving when the target moves.
            if (link.unique && (link.type === 'document' || link.type === 'media')) {
                return `umb://${link.type}/${link.unique.replace(/-/g, '')}`;
            }

            return link.url ?? null;
        } catch {
            return null;
        }
    }
}

export default N3oEditorJsElement;

declare global {
    interface HTMLElementTagNameMap {
        [elementName]: N3oEditorJsElement;
    }
}
