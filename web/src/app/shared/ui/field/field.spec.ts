import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Field } from './field';
import { Input } from './input';

@Component({
  imports: [Field, Input],
  template: `
    <app-field label="Email" [hint]="hint()" [error]="error()">
      <input appInput type="email" />
    </app-field>
  `,
})
class FieldHost {
  readonly hint = signal<string | null>(null);
  readonly error = signal<string | null>(null);
}

describe('Field and Input', () => {
  function render(setup?: (host: FieldHost) => void) {
    const fixture = TestBed.createComponent(FieldHost);
    setup?.(fixture.componentInstance);
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    return {
      fixture,
      root,
      input: root.querySelector('input') as HTMLInputElement,
      label: root.querySelector('label') as HTMLLabelElement,
    };
  }

  it('render_Label_IsLinkedToTheControl', () => {
    const { input, label } = render();

    expect(input.id).not.toBe('');
    expect(label.htmlFor).toBe(input.id);
  });

  it('render_ValidControl_HasNoInvalidOrDescribedBy', () => {
    const { input } = render();

    expect(input.getAttribute('aria-invalid')).toBeNull();
    expect(input.getAttribute('aria-describedby')).toBeNull();
    expect(input.classList).toContain('border-field-border');
  });

  it('render_Hint_DescribesTheControl', () => {
    const { root, input } = render((host) => host.hint.set('Dashes are added for you.'));

    const hint = root.querySelector(`#${input.getAttribute('aria-describedby')}`);
    expect(hint?.textContent).toContain('Dashes are added for you.');
  });

  it('render_Error_MarksInvalidAndLinksErrorBeforeHint', () => {
    const { root, input } = render((host) => {
      host.hint.set('12–128 characters');
      host.error.set('Use at least 12 characters');
    });

    const [errorId, hintId] = (input.getAttribute('aria-describedby') ?? '').split(' ');
    expect(input.getAttribute('aria-invalid')).toBe('true');
    expect(input.classList).toContain('border-invalid');
    expect(root.querySelector(`#${errorId}`)?.textContent).toContain('Use at least 12 characters');
    expect(root.querySelector(`#${hintId}`)?.textContent).toContain('12–128 characters');
  });

  it('render_ErrorRemoved_ClearsTheInvalidState', () => {
    const { fixture, input } = render((host) => host.error.set('Enter a valid email address'));

    fixture.componentInstance.error.set(null);
    fixture.detectChanges();

    expect(input.getAttribute('aria-invalid')).toBeNull();
    expect(input.getAttribute('aria-describedby')).toBeNull();
  });
});
