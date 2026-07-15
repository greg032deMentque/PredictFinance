import { DestroyRef, Directive, ElementRef, Input, OnInit, Renderer2, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { GlossaryService } from '../../services/glossary.service';

@Directive({
  selector: '[appGlossaryTerm]',
  standalone: true
})
export class GlossaryTermDirective implements OnInit {

  @Input({ required: true }) appGlossaryTerm!: string;

  private readonly glossaryService = inject(GlossaryService);
  private readonly el = inject(ElementRef);
  private readonly renderer = inject(Renderer2);
  private readonly destroyRef = inject(DestroyRef);

  private tooltipEl: HTMLElement | null = null;
  private tooltipId = `glossary-tip-${Math.random().toString(36).slice(2, 9)}`;

  ngOnInit(): void {
    this.glossaryService.getGlossary().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      const term = this.glossaryService.lookup(this.appGlossaryTerm);
      if (!term) {
        return;
      }

      this.applyHostStyles();
      this.createTooltip(term.Definition);
      this.bindEvents();
    });
  }

  private applyHostStyles(): void {
    const host: HTMLElement = this.el.nativeElement;
    this.renderer.setStyle(host, 'text-decoration', 'underline dotted');
    this.renderer.setStyle(host, 'text-decoration-color', 'currentColor');
    this.renderer.setStyle(host, 'cursor', 'help');
    this.renderer.setStyle(host, 'position', 'relative');
    this.renderer.setAttribute(host, 'tabindex', '0');
    this.renderer.setAttribute(host, 'role', 'button');
    this.renderer.setAttribute(host, 'aria-describedby', this.tooltipId);
  }

  private createTooltip(definition: string): void {
    const tip: HTMLElement = this.renderer.createElement('span');
    this.renderer.setAttribute(tip, 'id', this.tooltipId);
    this.renderer.setAttribute(tip, 'role', 'tooltip');

    this.renderer.setStyle(tip, 'position', 'absolute');
    this.renderer.setStyle(tip, 'bottom', 'calc(100% + 6px)');
    this.renderer.setStyle(tip, 'left', '50%');
    this.renderer.setStyle(tip, 'transform', 'translateX(-50%)');
    this.renderer.setStyle(tip, 'background', '#1e293b');
    this.renderer.setStyle(tip, 'color', '#f8fafc');
    this.renderer.setStyle(tip, 'padding', '6px 10px');
    this.renderer.setStyle(tip, 'border-radius', '6px');
    this.renderer.setStyle(tip, 'font-size', '0.78rem');
    this.renderer.setStyle(tip, 'font-weight', '400');
    this.renderer.setStyle(tip, 'line-height', '1.4');
    this.renderer.setStyle(tip, 'max-width', '260px');
    this.renderer.setStyle(tip, 'min-width', '140px');
    this.renderer.setStyle(tip, 'white-space', 'normal');
    this.renderer.setStyle(tip, 'z-index', '1080');
    this.renderer.setStyle(tip, 'pointer-events', 'none');
    this.renderer.setStyle(tip, 'box-shadow', '0 4px 12px rgba(0,0,0,0.25)');
    this.renderer.setStyle(tip, 'display', 'none');

    const text = this.renderer.createText(definition);
    this.renderer.appendChild(tip, text);
    this.renderer.appendChild(this.el.nativeElement, tip);
    this.tooltipEl = tip;
  }

  private bindEvents(): void {
    const host: HTMLElement = this.el.nativeElement;

    this.renderer.listen(host, 'mouseenter', () => this.show());
    this.renderer.listen(host, 'mouseleave', () => this.hide());
    this.renderer.listen(host, 'focus', () => this.show());
    this.renderer.listen(host, 'blur', () => this.hide());
  }

  private show(): void {
    if (this.tooltipEl) {
      this.renderer.setStyle(this.tooltipEl, 'display', 'block');
    }
  }

  private hide(): void {
    if (this.tooltipEl) {
      this.renderer.setStyle(this.tooltipEl, 'display', 'none');
    }
  }
}
