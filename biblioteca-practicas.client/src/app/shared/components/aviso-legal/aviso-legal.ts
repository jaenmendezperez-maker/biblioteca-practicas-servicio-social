import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-aviso-legal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './aviso-legal.html'
})
export class AvisoLegalComponent {
  @Input() variante: 'compacto' | 'completo' = 'compacto';
}