import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { GhostLevel } from './community.models';
import { GhostBadge } from './ghost-badge';

@Component({
  imports: [GhostBadge],
  template: `<app-ghost-badge [level]="level()" [score]="score()" />`,
})
class BadgeHost {
  readonly level = signal<GhostLevel>('High');
  readonly score = signal(4);
}

describe('GhostBadge', () => {
  function create(): { host: BadgeHost; badge: HTMLElement } {
    const fixture = TestBed.createComponent(BadgeHost);
    fixture.detectChanges();
    const badge = (fixture.nativeElement as HTMLElement).querySelector('app-ghost-badge')!;
    return { host: fixture.componentInstance, badge: badge as HTMLElement };
  }

  it('render_High_ReadsLevelAndScoreAndUsesTheDangerColour', () => {
    const { badge } = create();

    expect(badge.textContent?.replace(/\s+/g, ' ').trim()).toBe('Ghost risk High, 4 of 5');
    expect(badge.querySelector('.sr-only')?.textContent).toContain('4 of 5');
    expect(badge.classList).toContain('border-danger');
    expect(badge.classList).toContain('border-dashed');
  });

  it('render_Score_FillsThatManyWaves', () => {
    const { badge } = create();

    const waves = Array.from(badge.querySelectorAll('path'));
    expect(waves.length).toBe(5);
    expect(waves.filter((wave) => wave.classList.contains('stroke-danger')).length).toBe(4);
    expect(waves.filter((wave) => wave.classList.contains('stroke-rule-strong')).length).toBe(1);
  });

  it('render_Medium_UsesTheNoResponseColour', () => {
    const fixture = TestBed.createComponent(BadgeHost);
    fixture.componentInstance.level.set('Medium');
    fixture.componentInstance.score.set(2);
    fixture.detectChanges();
    const badge = (fixture.nativeElement as HTMLElement).querySelector('app-ghost-badge')!;

    expect(badge.classList).toContain('border-status-no-response');
    expect(badge.querySelectorAll('path.stroke-status-no-response').length).toBe(2);
  });
});
